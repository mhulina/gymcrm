using FluentAssertions;
using GymCRM.SchedulingAPI.Infrastructure.Implementation;
using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models.Entities;
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

        var seeder = new HolidaySeeder(new HttpClient(), _context);
        seeder.SeedAsync("HR", DateTime.UtcNow.Year).Wait();
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

    [Fact]
    public async Task GivenHolidaysSeeded_WhenGettingAll_ThenAllHolidaysAreReturned()
    {
        // When
        var result = await _holidayRepository.GetAllAsync(CancellationToken.None);

        // Then
        result.Should().HaveCount(14);
    }

    [Fact]
    public async Task GivenValidId_WhenGettingById_ThenTheMatchingHolidayIsReturned()
    {
        // Given
        var existing = (await _holidayRepository.GetAllAsync(CancellationToken.None)).First();

        // When
        var result = await _holidayRepository.GetByIdAsync(existing.Id, CancellationToken.None);

        // Then
        result.Should().NotBeNull();
        result.EnglishName.Should().Be(existing.EnglishName);
    }

    [Fact]
    public async Task GivenNewHoliday_WhenAdding_ThenHolidayIsPersisted()
    {
        // Given
        var holiday = new Holiday
        {
            Id = Guid.CreateVersion7(),
            EnglishName = "Test Holiday",
            LocalName = "Test Holiday",
            CountryCode = "HR",
            Date = DateTime.SpecifyKind(new DateTime(DateTime.UtcNow.Year, 1, 15), DateTimeKind.Utc),
            Type = "Public",
            RegionCode = "",
            Year = DateTime.UtcNow.Year,
            Created = DateTime.UtcNow
        };

        // When
        _holidayRepository.Add(holiday);
        var result = await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Then
        result.Should().BeTrue();
        var fetched = await _holidayRepository.GetByIdAsync(holiday.Id, CancellationToken.None);
        fetched.Should().NotBeNull();
        fetched.EnglishName.Should().Be("Test Holiday");
    }

    [Fact]
    public async Task GivenExistingHoliday_WhenUpdating_ThenChangesArePersisted()
    {
        // Given
        var existing = (await _holidayRepository.GetAllAsync(CancellationToken.None)).First();

        // When
        existing.LocalName = "Updated Local Name";
        _holidayRepository.Update(existing);
        var result = await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Then
        result.Should().BeTrue();
        var updated = await _holidayRepository.GetByIdAsync(existing.Id, CancellationToken.None);
        updated.LocalName.Should().Be("Updated Local Name");
    }

    [Fact]
    public async Task GivenExistingHoliday_WhenDeleting_ThenHolidayIsRemoved()
    {
        // Given
        var existing = (await _holidayRepository.GetAllAsync(CancellationToken.None)).First();

        // When
        _holidayRepository.Delete(existing);
        var result = await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Then
        result.Should().BeTrue();
        var afterDelete = await _holidayRepository.GetByIdAsync(existing.Id, CancellationToken.None);
        afterDelete.Should().BeNull();
    }
}