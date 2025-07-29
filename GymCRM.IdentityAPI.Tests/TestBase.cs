using GymCRM.IdentityAPI.Models;
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

namespace GymCRM.MembershipAPI.Tests;

public class TestBase : IDisposable
{
    private string _testDbConnectionString;
    private string _connectionStringWithoutDb;
    private IConfiguration _configuration;
    protected readonly AppDbContext _context;
    
    public IServiceProvider ServiceProvider { get; private set; }
    
    protected TestBase()
    {
        LoadConfiguration();
        EnsureDatabaseExistsAndMigrate();

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
        _context = ServiceProvider.GetService<AppDbContext>();
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

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_testDbConnectionString)
            .Options;

        using var dbContext = new AppDbContext(options);
        dbContext.Database.Migrate();
    }
    
    protected void ClearDatabase()
    {
        var entityTypes = _context.Model.GetEntityTypes();

        foreach (var entityType in entityTypes)
        {
            var clrType = entityType.ClrType;

            // Get the DbSet dynamically
            var dbSet = _context.GetType()
                .GetMethod("Set", Type.EmptyTypes)
                .MakeGenericMethod(clrType)
                .Invoke(_context, null);

            // Get the entities to remove
            var entities = ((IQueryable)dbSet).Cast<object>().ToList();

            _context.RemoveRange(entities);
        }

        _context.SaveChanges();
    }

    public void Dispose()
    {
        ClearDatabase();
        _context.Dispose();
    }
}