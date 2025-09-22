using FluentAssertions;
using GymCRM.SchedulingAPI.Infrastructure.Implementation;
using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Services;

namespace GymCRM.SchedulingAPI.Tests.Integration.Repositories;

public class TestHolidayRepository : TestBase
{
    private readonly IHolidayRepository _holidayRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TestHolidayRepository()
    {
        _holidayRepository = new HolidayRepository(_context);
        _unitOfWork = new UnitOfWork(_context);
    }

    [Fact]
    public async Task GivenCurrentYear_WhenGettingByYear_ThenProperHolidaysAreReturned()
    {
        // Given
        var year = DateTime.Now.Year;
        var christmas = DateTime.Parse($"{year}-12-25");
        
        // When
        var result = await _holidayRepository.GetByYearAsync(year, CancellationToken.None);
        
        // Then
        result.Should().NotBeNullOrEmpty();
        result.Should().HaveCount(14);
        result.Should().Contain(x => x.Date == christmas);
    }

    [Fact]
    public async Task GivenValidDateOfAHoliday_WhenGettingByDate_ThenProperHolidayIsReturned()
    {
        // Given
        var date = DateTime.Parse($"{DateTime.UtcNow.Year}-11-01");
        
        // When
        var result = await _holidayRepository.GetByDateAsync(date, CancellationToken.None);
        
        // Then
        result.Should().NotBeNull();
        result.EnglishName.Should().Be("All Saints' Day");
        result.CountryCode.Should().Be("HR");
        result.Type.Should().Be("Public");
    }

    [Theory]
    [InlineData(12, 2)]
    [InlineData(4, 2)]
    [InlineData(3, 0)]
    public async Task GivenMonth_WhenGettingByMonth_ThenExpectedCountOfHolidaysAreReturned(
        int month,
        int expectedCountOfHolidays)
    {
        // Given
        var year = DateTime.UtcNow.Year;
        
        // When
        var result = await _holidayRepository.GetByMonthAsync(month, year, CancellationToken.None);
        
        // Then
        result.Count.Should().Be(expectedCountOfHolidays);
    }
}