using GymCRM.IdentityAPI;
using GymCRM.IdentityAPI.Infrastructure;
using Microsoft.AspNetCore.Connections;
using Microsoft.EntityFrameworkCore;
using Serilog;

using var log = new LoggerConfiguration()
	.MinimumLevel.Debug()
	.WriteTo.Console()
	.WriteTo.File("./logs/IdentityAPI/logs.txt", rollingInterval: RollingInterval.Day)
	.CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.SetupConfiguration();

builder.Host.UseSerilog(logger: log);

// Add services to the container.
builder
	.SetupContext().Services
	.SetupCors()
	.SetupAutoMapper();

var secretForKey = builder.Configuration["Authentication:SecretForKey"];

if (string.IsNullOrEmpty(secretForKey))
{
	throw new InvalidOperationException("Secret is missing from configuration");
}

builder.Services
	.SetupAuthentication(builder, secretForKey)
	.SetupRateLimiting()
	.SetupDependencyInjection()
	.SetupApiVersioning();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services
	.AddEndpointsApiExplorer()
	.SetupSwagger()
	.AddHealthChecks();

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app
	.UseCors("AllowAny")
	.UseHttpsRedirection()
	.UseAuthentication()
	.UseAuthorization();

app.MapHealthChecks("/health");

app.MapControllers();
ApplyMigration();
app.Run();


void ApplyMigration()
{
	using (var scope = app.Services.CreateScope())
	{
		var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

		if (!db.Database.CanConnect())
		{
			throw new ConnectionAbortedException("Database connection could not be established");
		}
		if (db.Database.GetPendingMigrations().Any())
		{
			db.Database.Migrate();
		}
	}
}