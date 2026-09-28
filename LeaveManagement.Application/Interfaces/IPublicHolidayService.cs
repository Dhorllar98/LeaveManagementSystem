using LeaveManagement.Application.DTOs.PublicHoliday;

namespace LeaveManagement.Application.Interfaces;

public interface IPublicHolidayService
{
    Task<int> SyncPublicHolidaysAsync(Guid organizationId, int year, string countryCode, CancellationToken cancellationToken = default);
}