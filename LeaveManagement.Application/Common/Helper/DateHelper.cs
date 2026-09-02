namespace LeaveManagement.Application.Common.Helpers;

public static class DateHelper
{
    /// <summary>
    /// Calculates working days (Mon-Fri) excluding weekends and public holidays.
    /// </summary>
    public static int CalculateBusinessDays(
        DateTime startDate,
        DateTime endDate,
        HashSet<DateTime>? publicHolidays = null)
    {
        if (startDate > endDate)
            return 0;

        int businessDays = 0;
        DateTime currentDate = startDate.Date;
        DateTime targetDate = endDate.Date;
        publicHolidays ??= new HashSet<DateTime>();

        while (currentDate <= targetDate)
        {
            bool isWeekend = currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday;
            bool isPublicHoliday = publicHolidays.Contains(currentDate);

            if (!isWeekend && !isPublicHoliday)
            {
                businessDays++;
            }
            currentDate = currentDate.AddDays(1);
        }

        return businessDays;
    }
}