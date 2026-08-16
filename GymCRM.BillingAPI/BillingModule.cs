using AutoMapper;
using GymCRM.BillingAPI.Infrastructure;
using GymCRM.BillingAPI.Infrastructure.Implementation;
using GymCRM.BillingAPI.Infrastructure.Interface;
using GymCRM.BillingAPI.Models.Interface;
using GymCRM.BillingAPI.Services.Implementation;
using GymCRM.BillingAPI.Services.Interface;
using Microsoft.AspNetCore.Connections;
using Microsoft.EntityFrameworkCore;

namespace GymCRM.BillingAPI;

/// <summary>
/// Composition root for the Billing module. Owns everything specific to this module
/// (DbContext, repositories, services, controllers, entity mappings). Cross-cutting host concerns
/// (auth, CORS, versioning, Swagger, rate limiting) live in the host project.
/// </summary>
public static class BillingModule
{
    public static IServiceCollection AddBillingModule(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddBillingDbContext(configuration.GetConnectionString("Billing"))
            .AddBillingServices();

    public static IServiceCollection AddBillingDbContext(this IServiceCollection services, string? connectionString)
    {
        services.AddDbContext<BillingDbContext>(option => option.UseNpgsql(connectionString));

        return services;
    }

    public static IServiceCollection AddBillingServices(this IServiceCollection services)
    {
        services
            .AddScoped<IUnitOfWork, UnitOfWork>()
            .AddScoped<ISubscriptionsRepository, SubscriptionsRepository>()
            .AddScoped<IPaymentsRepository, PaymentsRepository>()
            .AddScoped<ISubscriptionsService, SubscriptionsService>()
            .AddScoped<IPaymentsService, PaymentsService>();

        return services;
    }

    public static IMvcBuilder AddBillingControllers(this IMvcBuilder builder) =>
        builder.AddApplicationPart(typeof(BillingModule).Assembly);

    public static void ConfigureBillingMappings(IMapperConfigurationExpression config)
    {
        config.CreateMap<Models.Entities.Subscription, Models.DTOs.Subscription>();
        config.CreateMap<Models.DTOs.Subscription, Models.Entities.Subscription>();
        config.CreateMap<Models.Entities.Payment, Models.DTOs.Payment>();
        config.CreateMap<Models.DTOs.Payment, Models.Entities.Payment>();
    }

    public static async Task ApplyBillingMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        if (!db.Database.CanConnect())
        {
            throw new ConnectionAbortedException("Database connection could not be established");
        }

        if ((await db.Database.GetPendingMigrationsAsync()).Any())
        {
            await db.Database.MigrateAsync();
        }
    }
}
