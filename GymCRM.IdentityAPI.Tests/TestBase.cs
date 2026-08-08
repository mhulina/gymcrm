using GymCRM.IdentityAPI.Infrastructure;
using GymCRM.IdentityAPI.Infrastructure.Implementation;
using GymCRM.IdentityAPI.Infrastructure.Interface;
using GymCRM.IdentityAPI.Models.Implementation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using GymCRM.IdentityAPI.Models.Interface;
using GymCRM.IdentityAPI.Services.Implementation;
using GymCRM.IdentityAPI.Services.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using AuthenticationService = GymCRM.IdentityAPI.Services.Implementation.AuthenticationService;
using IAuthenticationService = GymCRM.IdentityAPI.Services.Interface.IAuthenticationService;

namespace GymCRM.IdentityAPI.Tests;

public class TestBase : IDisposable
{
    private string _testDbConnectionString;
    private string _connectionStringWithoutDb;
    private IConfiguration _configuration;
    protected readonly IdentityDbContext _context;
    
    public IServiceProvider ServiceProvider { get; private set; }
    
    protected TestBase()
    {
        LoadConfiguration();
        EnsureDatabaseExistsAndMigrate();

        var services = new ServiceCollection();
        
        services.AddSingleton<IConfiguration>(_configuration);
        
        services.AddDbContext<IdentityDbContext>(options => 
            options.UseNpgsql(_testDbConnectionString));

        services.AddIdentityServices();
        
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
        _context = ServiceProvider.GetService<IdentityDbContext>();
    }

    private void LoadConfiguration()
    {
        _configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .AddEnvironmentVariables()
            .Build();
        
        _testDbConnectionString = _configuration.GetConnectionString("DefaultConnection");

        // Strip the Database part from the connection string to connect to the server for DB creation
        var builder = new NpgsqlConnectionStringBuilder(_testDbConnectionString)
        {
            Database = "postgres"
        };
        _connectionStringWithoutDb = builder.ToString();
    }
    
    private void EnsureDatabaseExistsAndMigrate()
    {
        var builder = new NpgsqlConnectionStringBuilder(_testDbConnectionString);
        var dbName = builder.Database;

        using var conn = new NpgsqlConnection(_connectionStringWithoutDb);
        conn.Open();

        using (var cmd = new NpgsqlCommand($"SELECT 1 FROM pg_database WHERE datname = '{dbName}';", conn))
        {
            var exists = cmd.ExecuteScalar();
            if (exists == null)
            {
                using var createCmd = new NpgsqlCommand($"CREATE DATABASE \"{dbName}\";", conn);
                createCmd.ExecuteNonQuery();
            }
        }

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(_testDbConnectionString)
            .Options;

        using var dbContext = new IdentityDbContext(options);
        dbContext.Database.Migrate();
    }
    
    protected void ClearDatabase()
    {
        try
        {
            // Use TRUNCATE CASCADE to handle foreign keys automatically
            var schema = _context.Model.GetDefaultSchema() ?? "identity_db";
        
            // Get all table names
            var tableNames = _context.Model.GetEntityTypes()
                .Where(et => !et.IsOwned())
                .Select(et => et.GetTableName())
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToList();

            // Truncate all tables with CASCADE
            foreach (var tableName in tableNames)
            {
                try
                {
                    _context.Database.ExecuteSqlRaw(
                        $"TRUNCATE TABLE \"{schema}\".\"{tableName}\" RESTART IDENTITY CASCADE");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not truncate {tableName}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing database with TRUNCATE, trying manual deletion: {ex.Message}");
        
            // Fallback: Manual deletion in correct order
            try
            {
                // Delete Members first (child table)
                var members = _context.Members.ToList();
                if (members.Any())
                {
                    _context.Members.RemoveRange(members);
                }
            
                // Delete Accounts second (parent table)
                var accounts = _context.Accounts.ToList();
                if (accounts.Any())
                {
                    _context.Accounts.RemoveRange(accounts);
                }
            
                _context.SaveChanges();
            }
            catch (Exception innerEx)
            {
                Console.WriteLine($"Error in fallback deletion: {innerEx.Message}");
                // Don't throw - let the test framework handle it
            }
        }
    }

    public void Dispose()
    {
        ClearDatabase();
        _context.Dispose();
    }
}