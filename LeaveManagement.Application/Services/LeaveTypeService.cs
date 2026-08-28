using LeaveManagement.Application.DTOs.LeaveType;
using LeaveManagement.Application.Interfaces;
using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Interfaces;

namespace LeaveManagement.Application.Services;

public class LeaveTypeService : ILeaveTypeService
{
    private readonly ILeaveTypeRepository _repository;
    private readonly IUserRepository _userRepository;
    private readonly ILeaveAllocationRepository _allocationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LeaveTypeService(
        ILeaveTypeRepository repository,
        IUserRepository userRepository,
        ILeaveAllocationRepository allocationRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _userRepository = userRepository;
        _allocationRepository = allocationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<LeaveTypeDto>> GetLeaveTypesAsync(Guid orgId, CancellationToken cancellationToken = default)
    {
        var leaveTypes = await _repository.GetAllByOrganizationAsync(orgId, cancellationToken);
        return leaveTypes.Select(lt => new LeaveTypeDto
        {
            Id = lt.Id,
            Name = lt.Name,
            DefaultDays = lt.DefaultDays
        });
    }

    public async Task<LeaveTypeDto?> GetLeaveTypeByIdAsync(Guid id, Guid orgId, CancellationToken cancellationToken = default)
    {
        var leaveType = await _repository.GetByIdAsync(id, cancellationToken);
        if (leaveType == null || leaveType.OrganizationId != orgId) return null;

        return new LeaveTypeDto
        {
            Id = leaveType.Id,
            Name = leaveType.Name,
            DefaultDays = leaveType.DefaultDays
        };
    }

    public async Task<LeaveTypeDto> CreateLeaveTypeAsync(CreateLeaveTypeDto dto, Guid orgId, CancellationToken cancellationToken = default)
    {
        var leaveType = new LeaveType
        {
            OrganizationId = orgId,
            Name = dto.Name,
            DefaultDays = dto.DefaultDays
        };

        await _repository.AddAsync(leaveType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Auto-allocate this new LeaveType for all existing organization employees in the current year
        var orgUsers = await _userRepository.GetAllByOrganizationAsync(orgId, cancellationToken);
        int currentYear = DateTime.UtcNow.Year;

        foreach (var user in orgUsers)
        {
            var allocation = new LeaveAllocation
            {
                EmployeeId = user.Id,
                LeaveTypeId = leaveType.Id,
                NumberOfDays = leaveType.DefaultDays,
                Period = currentYear
            };
            await _allocationRepository.AddAsync(allocation, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LeaveTypeDto
        {
            Id = leaveType.Id,
            Name = leaveType.Name,
            DefaultDays = leaveType.DefaultDays
        };
    }

    public async Task<bool> UpdateLeaveTypeAsync(Guid id, UpdateLeaveTypeDto dto, Guid orgId, CancellationToken cancellationToken = default)
    {
        var leaveType = await _repository.GetByIdAsync(id, cancellationToken);
        if (leaveType == null || leaveType.OrganizationId != orgId) return false;

        leaveType.Name = dto.Name;
        leaveType.DefaultDays = dto.DefaultDays;
        leaveType.UpdatedAt = DateTime.UtcNow;

        _repository.Update(leaveType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteLeaveTypeAsync(Guid id, Guid orgId, CancellationToken cancellationToken = default)
    {
        var leaveType = await _repository.GetByIdAsync(id, cancellationToken);
        if (leaveType == null || leaveType.OrganizationId != orgId) return false;

        _repository.Delete(leaveType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}