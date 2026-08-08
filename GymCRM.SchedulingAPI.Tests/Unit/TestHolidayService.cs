using AutoMapper;
using FluentAssertions;
using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models.Entities;
using GymCRM.SchedulingAPI.Services.Implementation;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GymCRM.SchedulingAPI.Tests.Unit;

public class TestHolidayService
{
    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task GivenInvalidMonth_WhenFetchingHolidaysForMonth_ThenArgumentExceptionIsThrown(int month)
    {
        // Given
        var service = CreateHolidayService();

        // When
        Func<Task> act = () => service.FetchHolidaysForMonth(month, 2024);

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenValidMonth_WhenFetchingHolidaysForMonth_ThenMappedHolidaysAreReturned()
    {
        // Given
        var holiday = CreateHoliday("New Year's Day");
        var repositoryMock = new Mock<IHolidayRepository>();
        repositoryMock.Setup(x => x.GetByMonthAsync(1, 2024, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Holiday> { holiday });
        var service = CreateHolidayService(repositoryMock.Object);

        // When
        var result = await service.FetchHolidaysForMonth(1, 2024);

        // Then
        result.Should().ContainSingle(h => h.EnglishName == "New Year's Day");
    }

    [Fact]
    public async Task GivenHolidaysExist_WhenFetchingAllHolidays_ThenMappedHolidaysAreReturned()
    {
        // Given
        var holidays = new List<Holiday> { CreateHoliday("New Year's Day"), CreateHoliday("Christmas Day") };
        var repositoryMock = new Mock<IHolidayRepository>();
        repositoryMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(holidays);
        var service = CreateHolidayService(repositoryMock.Object);

        // When
        var result = await service.FetchAllHolidays();

        // Then
        result.Should().HaveCount(2);
        result.Select(h => h.EnglishName).Should().Contain(new[] { "New Year's Day", "Christmas Day" });
    }

    private static Holiday CreateHoliday(string englishName) => new()
    {
        Id = Guid.NewGuid(),
        EnglishName = englishName,
        LocalName = englishName,
        CountryCode = "US",
        Date = DateTime.UtcNow,
        Type = "Public",
        Year = DateTime.UtcNow.Year,
        Created = DateTime.UtcNow
    };

    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(SchedulingModule.ConfigureSchedulingMappings);

        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static HolidayService CreateHolidayService(IHolidayRepository? repository = null, IMapper? mapper = null) =>
        new(repository ?? Mock.Of<IHolidayRepository>(), mapper ?? CreateMapper());
}
