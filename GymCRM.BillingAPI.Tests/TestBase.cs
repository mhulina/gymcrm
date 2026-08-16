using GymCRM.BillingAPI;
using GymCRM.BillingAPI.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace GymCRM.BillingAPI.Tests;

public class TestBase : IDisposable
{
    private readonly string _testDbConnectionString;
    private readonly string _connectionStringWithoutDb;
    private readonly IConfiguration _configuration;
    protected readonly BillingDbContext _context;

    public IServiceProvider ServiceProvider { get; private set; }

    protected TestBase()
    {
        _configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .AddEnvironmentVariables()
            .Build();

        _testDbConnectionString = _configuration.GetConnectionString("DefaultConnection");

        var builder = new NpgsqlConnectionStringBuilder(_testDbConnectionString)
        {
            Database = "postgres"
        };
        _connectionStringWithoutDb = builder.ToString();

        EnsureDatabaseExistsAndMigrate();

        var services = new ServiceCollection();
        services.AddSingleton(_configuration);

        services.AddDbContext<BillingDbContext>(options =>
            options.UseNpgsql(_testDbConnectionString));

        // Register Billing Repositories & Services
        services.AddLogging();
        services.AddBillingServices();
        services.AddAutoMapper(BillingModule.ConfigureBillingMappings);

        ServiceProvider = services.BuildServiceProvider();
        _context = ServiceProvider.GetRequiredService<BillingDbContext>();
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

        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseNpgsql(_testDbConnectionString)
            .Options;

        using var dbContext = new BillingDbContext(options);
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
