using LeaveManagement.Domain.Entities;

namespace LeaveManagement.Domain.Interfaces;

public interface IOrganizationRepository
{
    Task<bool> ExistsByPrefixAsync(string codePrefix, CancellationToken cancellationToken = default);
    Task AddAsync(Organization organization, CancellationToken cancellationToken = default);
}