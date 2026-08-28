using LeaveManagement.Application.DTOs.LeaveAllocation;
using LeaveManagement.Application.Interfaces;
using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Enums;
using LeaveManagement.Domain.Interfaces;

namespace LeaveManagement.Application.Services;

public class LeaveAllocationService : ILeaveAllocationService
{
    private readonly ILeaveAllocationRepository _repository;
    private readonly IUserRepository _userRepository;
    private readonly ILeaveTypeRepository _leaveTypeRepository;
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LeaveAllocationService(
        ILeaveAllocationRepository repository,
        IUserRepository userRepository,
        ILeaveTypeRepository leaveTypeRepository,
        ILeaveRequestRepository leaveRequestRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _userRepository = userRepository;
        _leaveTypeRepository = leaveTypeRepository;
        _leaveRequestRepository = leaveRequestRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<LeaveAllocationDto>> GetAllocationsByOrganizationAsync(Guid orgId, CancellationToken cancellationToken = default)
    {
        var allocations = await _repository.GetAllByOrganizationAsync(orgId, cancellationToken);
        return allocations.Select(la => new LeaveAllocationDto
        {
            Id = la.Id,
            NumberOfDays = la.NumberOfDays,
            Period = la.Period,
            EmployeeId = la.EmployeeId,
            EmployeeName = la.Employee?.FullName ?? string.Empty,
            LeaveTypeId = la.LeaveTypeId,
            LeaveTypeName = la.LeaveType?.Name ?? string.Empty
        });
    }

    public async Task<LeaveAllocationDto?> GetAllocationByIdAsync(Guid id, Guid orgId, CancellationToken cancellationToken = default)
    {
        var allocation = await _repository.GetByIdAsync(id, cancellationToken);
        if (allocation == null || allocation.Employee?.OrganizationId != orgId) return null;

        return new LeaveAllocationDto
        {
            Id = allocation.Id,
            NumberOfDays = allocation.NumberOfDays,
            Period = allocation.Period,
            EmployeeId = allocation.EmployeeId,
            EmployeeName = allocation.Employee?.FullName ?? string.Empty,
            LeaveTypeId = allocation.LeaveTypeId,
            LeaveTypeName = allocation.LeaveType?.Name ?? string.Empty
        };
    }

    public async Task<(bool Success, string? ErrorMessage, LeaveAllocationDto? Data)> CreateAllocationAsync(CreateLeaveAllocationDto dto, Guid orgId, CancellationToken cancellationToken = default)
    {
        var targetEmployee = await _userRepository.GetByIdAsync(dto.EmployeeId, cancellationToken);
        if (targetEmployee == null || targetEmployee.OrganizationId != orgId)
        {
            return (false, "Invalid employee or employee does not belong to your organization.", null);
        }

        var exists = await _repository.AllocationExistsAsync(dto.EmployeeId, dto.LeaveTypeId, dto.Period, cancellationToken);
        if (exists)
        {
            return (false, "An allocation for this leave type and period already exists for this employee.", null);
        }

        var allocation = new LeaveAllocation
        {
            NumberOfDays = dto.NumberOfDays,
            Period = dto.Period,
            EmployeeId = dto.EmployeeId,
            LeaveTypeId = dto.LeaveTypeId
        };

        await _repository.AddAsync(allocation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdAllocation = await _repository.GetByIdAsync(allocation.Id, cancellationToken);

        var result = new LeaveAllocationDto
        {
            Id = createdAllocation!.Id,
            NumberOfDays = createdAllocation.NumberOfDays,
            Period = createdAllocation.Period,
            EmployeeId = createdAllocation.EmployeeId,
            EmployeeName = createdAllocation.Employee?.FullName ?? string.Empty,
            LeaveTypeId = createdAllocation.LeaveTypeId,
            LeaveTypeName = createdAllocation.LeaveType?.Name ?? string.Empty
        };

        return (true, null, result);
    }

    public async Task<bool> UpdateAllocationAsync(Guid id, UpdateLeaveAllocationDto dto, Guid orgId, CancellationToken cancellationToken = default)
    {
        var allocation = await _repository.GetByIdAsync(id, cancellationToken);
        if (allocation == null || allocation.Employee?.OrganizationId != orgId) return false;

        allocation.NumberOfDays = dto.NumberOfDays;

        _repository.Update(allocation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAllocationAsync(Guid id, Guid orgId, CancellationToken cancellationToken = default)
    {
        var allocation = await _repository.GetByIdAsync(id, cancellationToken);
        if (allocation == null || allocation.Employee?.OrganizationId != orgId) return false;

        _repository.Delete(allocation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IEnumerable<UserLeaveBalanceDto>> GetUserLeaveBalancesAsync(Guid userId, Guid orgId, int period, CancellationToken cancellationToken = default)
    {
        var leaveTypes = await _leaveTypeRepository.GetAllByOrganizationAsync(orgId, cancellationToken);
        if (!leaveTypes.Any()) return Enumerable.Empty<UserLeaveBalanceDto>();

        var userAllocations = await _repository.GetAllByOrganizationAsync(orgId, cancellationToken);
        var filteredAllocations = userAllocations.Where(a => a.EmployeeId == userId && a.Period == period).ToList();

        var approvedRequests = await _leaveRequestRepository.GetAllByOrganizationAsync(orgId, cancellationToken);
        var userApprovedRequests = approvedRequests.Where(r => r.EmployeeId == userId
                                                           && r.Status == LeaveStatus.Approved
                                                           && r.StartDate.Year == period).ToList();

        return leaveTypes.Select(lt =>
        {
            var allocation = filteredAllocations.FirstOrDefault(a => a.LeaveTypeId == lt.Id);
            int totalDays = allocation?.NumberOfDays ?? lt.DefaultDays;

            int daysUsed = userApprovedRequests
                .Where(r => r.LeaveTypeId == lt.Id)
                .Sum(r => (r.EndDate - r.StartDate).Days + 1);

            return new UserLeaveBalanceDto
            {
                LeaveTypeId = lt.Id,
                LeaveTypeName = lt.Name,
                TotalDays = totalDays,
                DaysUsed = daysUsed
            };
        });
    }
}