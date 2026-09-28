using System.Text.Json;
using System.Text.Json.Serialization;
using LeaveManagement.Application.Interfaces;
using LeaveManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LeaveManagement.Application.Services;

public class PublicHolidayService : IPublicHolidayService
{
    private readonly IAppDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PublicHolidayService> _logger;

    public PublicHolidayService(
        IAppDbContext context,
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<PublicHolidayService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<int> SyncPublicHolidaysAsync(
        Guid organizationId,
        int year,
        string countryCode,
        CancellationToken cancellationToken = default)
    {
        string apiKey = _configuration["GoogleCalendar:ApiKey"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Google Calendar API key is missing from configuration.");
            return 0;
        }

        string code = string.IsNullOrWhiteSpace(countryCode)
            ? (_configuration["GoogleCalendar:DefaultCountryCode"] ?? "ng")
            : countryCode.Trim().ToLower();

        // Google Holiday Calendar ID format: en.{countryCode}#holiday@group.v.calendar.google.com
        string calendarId = Uri.EscapeDataString($"en.{code}#holiday@group.v.calendar.google.com");

        DateTime timeMin = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime timeMax = new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        string url = $"https://www.googleapis.com/calendar/v3/calendars/{calendarId}/events" +
                     $"?key={apiKey}" +
                     $"&timeMin={timeMin:yyyy-MM-ddTHH:mm:ssZ}" +
                     $"&timeMax={timeMax:yyyy-MM-ddTHH:mm:ssZ}" +
                     $"&singleEvents=true" +
                     $"&orderBy=startTime";

        GoogleCalendarEventsResponse? googleResponse;
        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Google Calendar API request failed with status code {StatusCode}", response.StatusCode);
                return 0;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            googleResponse = JsonSerializer.Deserialize<GoogleCalendarEventsResponse>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch or deserialize holidays from Google Calendar API.");
            return 0;
        }

        if (googleResponse?.Items == null || !googleResponse.Items.Any())
        {
            return 0;
        }

        // Fetch existing holiday dates for this organization & year to prevent duplicates
        var existingDates = await _context.PublicHolidays
            .Where(ph => ph.OrganizationId == organizationId && ph.Date.Year == year)
            .Select(ph => ph.Date.Date)
            .ToListAsync(cancellationToken);

        int addedCount = 0;
        foreach (var item in googleResponse.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Start?.Date))
                continue;

            if (!DateTime.TryParse(item.Start.Date, out DateTime holidayDate))
                continue;

            holidayDate = holidayDate.Date;

            if (!existingDates.Contains(holidayDate))
            {
                _context.PublicHolidays.Add(new PublicHoliday
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = !string.IsNullOrWhiteSpace(item.Summary) ? item.Summary : "Public Holiday",
                    Date = holidayDate,
                    CreatedAt = DateTime.UtcNow
                });

                existingDates.Add(holidayDate); // Avoid duplicates within the same batch
                addedCount++;
            }
        }

        if (addedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return addedCount;
    }

    // Google API Response DTOs
    private record GoogleCalendarEventsResponse(
        [property: JsonPropertyName("items")] List<GoogleCalendarEvent>? Items);

    private record GoogleCalendarEvent(
        [property: JsonPropertyName("summary")] string? Summary,
        [property: JsonPropertyName("start")] EventDate? Start);

    private record EventDate(
        [property: JsonPropertyName("date")] string? Date);
}