using GymCRM.SchedulingAPI.Infrastructure;
using GymCRM.SchedulingAPI.Infrastructure.Implementation;
using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models.DTOs;
using GymCRM.SchedulingAPI.Services;
using GymCRM.SchedulingAPI.Services.Implementation;
using GymCRM.SchedulingAPI.Services.Interface;
using GymCRM.Shared.Utilities;
using AutoMapper;
using Microsoft.AspNetCore.Connections;
using Microsoft.EntityFrameworkCore;
using Holiday = GymCRM.SchedulingAPI.Models.Entities.Holiday;

namespace GymCRM.SchedulingAPI;

/// <summary>
/// Composition root for the Scheduling module. Owns everything specific to this module
/// (DbContext, repositories, services, entity mappings, controllers). Cross-cutting
/// host concerns (auth, CORS, versioning, Swagger, rate limiting) live in the host project.
/// </summary>
public static class SchedulingModule
{
    public static IServiceCollection AddSchedulingModule(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddSchedulingDbContext(configuration.GetConnectionString("Scheduling"))
            .AddSchedulingServices();

    public static IServiceCollection AddSchedulingDbContext(this IServiceCollection services, string? connectionString)
    {
        services.AddDbContext<SchedulingDbContext>(option => option.UseNpgsql(connectionString));

        return services;
    }

    public static IServiceCollection AddSchedulingServices(this IServiceCollection services)
    {
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<ITrainingSessionsRepository, TrainingSessionsRepository>();
        services.AddScoped<ITrainingSessionsService, TrainingSessionsService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ITrainerAvailabilitiesRepository, TrainerAvailabilitiesRepository>();
        services.AddScoped<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailabilitiesesRepository>();
        services.AddScoped<ITrainerWorkingHoursRepository, TrainerWorkingHoursRepository>();
        services.AddScoped<ITrainerAvailabilitiesService, TrainerAvailabilitiesService>();
        services.AddScoped<ITimeOffRepository, TimeOffRepository>();
        services.AddScoped<ITimeOffService, TimeOffService>();
        services.AddScoped<IHolidayRepository, HolidayRepository>();
        services.AddScoped<IHolidayService, HolidayService>();
        services.AddHttpClient<HolidaySeeder>();
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<IBookingValidationService, BookingValidationService>();

        return services;
    }

    public static void ConfigureSchedulingMappings(IMapperConfigurationExpression config)
    {
        config.CreateMap<TrainingSession, GymCRM.SchedulingAPI.Models.Entities.TrainingSession>();
        config.CreateMap<GymCRM.SchedulingAPI.Models.Entities.TrainingSession, TrainingSession>();
        config.CreateMap<TrainerAvailability, GymCRM.SchedulingAPI.Models.Entities.TrainerAvailability>();
        config.CreateMap<GymCRM.SchedulingAPI.Models.Entities.TrainerAvailability, TrainerAvailability>();
        config.CreateMap<TrainerDailyAvailability, GymCRM.SchedulingAPI.Models.Entities.TrainerDailyAvailability>();
        config.CreateMap<GymCRM.SchedulingAPI.Models.Entities.TrainerDailyAvailability, TrainerDailyAvailability>();
        config.CreateMap<TrainerWorkingHours, GymCRM.SchedulingAPI.Models.Entities.TrainerWorkingHours>();
        config.CreateMap<GymCRM.SchedulingAPI.Models.Entities.TrainerWorkingHours, TrainerWorkingHours>();
        config.CreateMap<GymCRM.SchedulingAPI.Models.Entities.TimeOff, TimeOff>();
        config.CreateMap<TimeOff, GymCRM.SchedulingAPI.Models.Entities.TimeOff>();
        config.CreateMap<Holiday, Models.DTOs.Holiday>();
        config.CreateMap<Models.DTOs.Holiday, Holiday>();
    }

    public static IMvcBuilder AddSchedulingControllers(this IMvcBuilder builder) =>
        builder.AddApplicationPart(typeof(SchedulingModule).Assembly);

    public static IMvcBuilder AddJsonTimeOnlyAndDateOnlyConverters(this IMvcBuilder builder)
    {
        builder.AddJsonOptions(opt =>
        {
            opt.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
            opt.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
        });

        return builder;
    }

    public static async Task ApplySchedulingMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();

        if (!db.Database.CanConnect())
        {
            throw new ConnectionAbortedException("Database connection could not be established");
        }

        var pendingMigrations = (await db.Database.GetPendingMigrationsAsync()).ToList();

        if (pendingMigrations.Count == 0)
        {
            return;
        }

        await db.Database.MigrateAsync();
    }

    public static async Task SeedSchedulingHolidaysAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<HolidaySeeder>();
        await seeder.SeedAsync("HR", DateTime.UtcNow.Year);
    }
}
