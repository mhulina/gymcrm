using GymCRM.IdentityAPI.Infrastructure;
using GymCRM.IdentityAPI.Infrastructure.Implementation;
using GymCRM.IdentityAPI.Infrastructure.Interface;
using GymCRM.IdentityAPI.Models.DTOs;
using GymCRM.IdentityAPI.Models.Implementation;
using GymCRM.IdentityAPI.Models.Interface;
using GymCRM.IdentityAPI.Services.Background;
using GymCRM.IdentityAPI.Services.Implementation;
using GymCRM.IdentityAPI.Services.Interface;
using AutoMapper;
using Microsoft.AspNetCore.Connections;
using Microsoft.EntityFrameworkCore;

namespace GymCRM.IdentityAPI;

/// <summary>
/// Composition root for the Identity module. Owns everything specific to this module
/// (DbContext, repositories, services, entity mappings, controllers). Cross-cutting
/// host concerns (auth, CORS, versioning, Swagger, rate limiting) live in the host project.
/// </summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddIdentityDbContext(configuration.GetConnectionString("Identity"))
            .AddIdentityServices();

    public static IServiceCollection AddIdentityDbContext(this IServiceCollection services, string? connectionString)
    {
        services.AddDbContext<IdentityDbContext>(option => option.UseNpgsql(connectionString));

        return services;
    }

    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services
            .AddScoped<IUnitOfWork, UnitOfWork>()
            .AddScoped<IMembersRepository, MembersRepository>()
            .AddScoped<IAccountsRepository, AccountsRepository>()
            .AddScoped<IMembersService, MembersService>()
            .AddScoped<IAuthenticationService, AuthenticationService>()
            .AddScoped<IRefreshTokensRepository, RefreshTokensRepository>()
            .AddScoped<IRefreshTokenService, RefreshTokenService>()
            .AddHostedService<RefreshTokenCleanupService>();

        return services;
    }

    public static void ConfigureIdentityMappings(IMapperConfigurationExpression config)
    {
        config.CreateMap<Member, GymCRM.IdentityAPI.Models.Entities.Member>();
        config.CreateMap<GymCRM.IdentityAPI.Models.Entities.Member, Member>();
    }

    public static IMvcBuilder AddIdentityControllers(this IMvcBuilder builder) =>
        builder.AddApplicationPart(typeof(IdentityModule).Assembly);

    public static async Task ApplyIdentityMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

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
