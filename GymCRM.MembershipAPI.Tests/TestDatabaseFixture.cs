using GymCRM.MembershipAPI.Infrastructure;
using GymCRM.MembershipAPI.Infrastructure.Implementation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using GymCRM.MembershipAPI.Infrastructure.Interface;
using GymCRM.MembershipAPI.Services.Implementation;
using GymCRM.MembershipAPI.Services.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using AuthenticationService = GymCRM.MembershipAPI.Services.Implementation.AuthenticationService;
using IAuthenticationService = GymCRM.MembershipAPI.Services.Interface.IAuthenticationService;

namespace GymCRM.MembershipAPI.Tests;

public class TestDatabaseFixture : IAsyncLifetime
{
    private string _testDbConnectionString;
    private string _connectionStringWithoutDb;
    private IConfiguration _configuration;
    
    public IServiceProvider ServiceProvider { get; private set; }
    
    public async Task InitializeAsync()
    {
        LoadConfiguration();
        await EnsureDatabaseExistsAndMigrateAsync();

        var services = new ServiceCollection();
        
        services.AddSingleton<IConfiguration>(_configuration);
        
        services.AddDbContext<AppDbContext>(options => 
            options.UseNpgsql(_testDbConnectionString));

        services.AddScoped<IMembersRepository, MembersRepository>();
        services.AddScoped<IMembersService, MembersService>();
        services.AddScoped<IAccountsRepository, AccountsRepository>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        services.Configure<AuthenticationOptions>(_configuration.GetSection("Authentication"));
        
        var secretForKey = _configuration["Authentication:SecretForKey"];
        
        if (string.IsNullOrEmpty(secretForKey))
        {
            throw new InvalidOperationException("Secret is missing from configuration");
        }
        
        services
            .AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(opt =>
            {
                opt.SaveToken = true;
                opt.RequireHttpsMetadata = false;
                opt.TokenValidationParameters = new()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _configuration["Authentication:Issuer"],
                    ValidAudience = _configuration["Authentication:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Convert.FromBase64String(secretForKey))
                };
            });
        
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("./logs/Tests/MembershipAPI.Tests/logs.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        services.AddSingleton<ILogger>(serilogLogger);
        services.AddLogging(lb => lb.AddSerilog(serilogLogger));
        
        ServiceProvider = services.BuildServiceProvider();
    }

    private void LoadConfiguration()
    {
        _configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .Build();
        
        _testDbConnectionString = _configuration.GetConnectionString("DefaultConnection");

        // Strip the Database part from the connection string to connect to the server for DB creation
        var builder = new NpgsqlConnectionStringBuilder(_testDbConnectionString)
        {
            Database = null
        };
        _connectionStringWithoutDb = builder.ToString();
    }
    
    private async Task EnsureDatabaseExistsAndMigrateAsync()
    {
        var builder = new NpgsqlConnectionStringBuilder(_testDbConnectionString);
        var dbName = builder.Database;

        await using var conn = new NpgsqlConnection(_connectionStringWithoutDb);
        await conn.OpenAsync();

        await using (var cmd = new NpgsqlCommand($"SELECT 1 FROM pg_database WHERE datname = '{dbName}';", conn))
        {
            var exists = await cmd.ExecuteScalarAsync();
            if (exists == null)
            {
                await using var createCmd = new NpgsqlCommand($"CREATE DATABASE \"{dbName}\";", conn);
                await createCmd.ExecuteNonQueryAsync();
            }
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_testDbConnectionString)
            .Options;

        await using var dbContext = new AppDbContext(options);
        await dbContext.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        // Optional: Drop the database after tests if you want full cleanup
        return Task.CompletedTask;
    }
}