using GymCRM.SchedulingAPI.Infrastructure;
using GymCRM.SchedulingAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Serilog;

namespace GymCRM.SchedulingAPI.Tests;

public class TestBase : IDisposable
{
    private readonly string _testDbConnectionString;
    private readonly string _connectionStringWithoutDb;
    private readonly IConfiguration _configuration;
    protected readonly SchedulingDbContext _context;

    public IServiceProvider ServiceProvider { get; private set; }

    protected TestBase()
    {
        _configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .Build();

        _testDbConnectionString = _configuration.GetConnectionString("DefaultConnection");

        var builder = new NpgsqlConnectionStringBuilder(_testDbConnectionString)
        {
            Database = null
        };
        _connectionStringWithoutDb = builder.ToString();

        EnsureDatabaseExistsAndMigrate();

        var services = new ServiceCollection();
        services.AddSingleton(_configuration);

        services.AddDbContext<SchedulingDbContext>(options =>
            options.UseNpgsql(_testDbConnectionString));

        // Register Scheduling Repositories & Services
        services.AddProjectServices();
        services.AutoMapper();

        // Logging
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("./logs/Tests/SchedulingAPI.Tests/logs.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        services.AddSingleton<ILogger>(serilogLogger);
        services.AddLogging(lb => lb.AddSerilog(serilogLogger));

        ServiceProvider = services.BuildServiceProvider();
        _context = ServiceProvider.GetRequiredService<SchedulingDbContext>();
        
        var seeder = new HolidaySeeder(new HttpClient(), _context);
        Task
            .Run(async () =>
                await seeder.SeedAsync("HR", DateTime.UtcNow.Year))
            .GetAwaiter()
            .GetResult();
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

        var options = new DbContextOptionsBuilder<SchedulingDbContext>()
            .UseNpgsql(_testDbConnectionString)
            .Options;

        using var dbContext = new SchedulingDbContext(options);
        dbContext.Database.Migrate();
    }

    protected void ClearDatabase()
    {
        var entityTypes = _context.Model.GetEntityTypes();

        foreach (var entityType in entityTypes)
        {
            var clrType = entityType.ClrType;
            var dbSet = _context.GetType()
                .GetMethod("Set", Type.EmptyTypes)
                .MakeGenericMethod(clrType)
                .Invoke(_context, null);

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
