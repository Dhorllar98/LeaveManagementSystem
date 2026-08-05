namespace LeaveManagement.Application.Common.Helpers;

public static class DateHelper
{
    /// <summary>
    /// Calculates the number of working days (Monday - Friday) between two dates inclusive.
    /// </summary>
    public static int CalculateBusinessDays(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
            return 0;

        int businessDays = 0;
        DateTime currentDate = startDate.Date;
        DateTime targetDate = endDate.Date;

        while (currentDate <= targetDate)
        {
            if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
            {
                businessDays++;
            }
            currentDate = currentDate.AddDays(1);
        }

        return businessDays;
    }
}