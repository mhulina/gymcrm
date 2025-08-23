using GymCRM.SchedulingAPI.Infrastructure;
using GymCRM.SchedulingAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymCRM.SchedulingAPI.Services;

public class HolidaySeeder
{
    private readonly HttpClient _httpClient;
    private readonly SchedulingDbContext _context;

    public HolidaySeeder(HttpClient httpClient, SchedulingDbContext context)
    {
        _httpClient = httpClient;
        _context = context;
    }

    public async Task SeedAsync(string countryCode, int year, CancellationToken cancellationToken = default)
    {
        var url = $"https://date.nager.at/api/v3/PublicHolidays/{year}/{countryCode}";
        var response = await _httpClient.GetFromJsonAsync<List<NagerHolidayDto>>(url, cancellationToken);

        if (response is null || !response.Any())
        {
            return;
        }

        foreach (var holiday in response)
        {
            bool exists = await _context.Holidays
                .AnyAsync(h => h.Date == DateTime.SpecifyKind(holiday.Date, DateTimeKind.Utc) 
                    && h.CountryCode == countryCode, cancellationToken);
            
            if (!exists)
            {
                _context.Holidays.Add(new Holiday
                {
                    Id = Guid.CreateVersion7(),
                    Date = DateTime.SpecifyKind(holiday.Date, DateTimeKind.Utc),
                    CountryCode = countryCode,
                    EnglishName = holiday.Name,
                    LocalName = holiday.LocalName,
                    Type = holiday.Global
                        ? "Public"
                        : "Regional",
                    RegionCode = holiday.Counties != null
                        ? string.Join(", ", holiday.Counties)
                        : string.Empty,
                    Year = year,
                    Created = DateTime.UtcNow
                });
            }
        }
        
        await _context.SaveChangesAsync(cancellationToken: cancellationToken);
    }
    
    public record NagerHolidayDto(
        DateTime Date,
        string LocalName,
        string Name,
        string CountryCode,
        bool Fixed,
        bool Global,
        string? Type,
        List<string>? Counties,
        int? LaunchYear);
}