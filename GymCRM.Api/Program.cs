using AutoMapper;
using GymCRM.Api;
using GymCRM.IdentityAPI;
using GymCRM.SchedulingAPI;
using Serilog;

using var log = new LoggerConfiguration()
	.MinimumLevel.Debug()
	.WriteTo.Console()
	.WriteTo.File("./logs/GymCRM.Api/logs.txt", rollingInterval: RollingInterval.Day)
	.CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(logger: log);

var secretForKey = builder.Configuration["Authentication:SecretForKey"];

if (string.IsNullOrEmpty(secretForKey))
{
	throw new InvalidOperationException("Secret is missing from configuration");
}

// Add services to the container. Each module owns its own DbContext/repositories/services;
// this host owns everything that can only be configured once per process.
builder.Services
	.AddIdentityModule(builder.Configuration)
	.AddSchedulingModule(builder.Configuration)
	.AddAutoMapper(cfg =>
	{
		IdentityModule.ConfigureIdentityMappings(cfg);
		SchedulingModule.ConfigureSchedulingMappings(cfg);
	})
	.SetupCors(builder.Configuration)
	.SetupAuthentication(builder.Configuration, secretForKey)
	.SetupRateLimiting()
	.SetupApiVersioning();

builder.Services
	.AddControllers()
	.AddIdentityControllers()
	.AddSchedulingControllers()
	.AddJsonTimeOnlyAndDateOnlyConverters();

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

await app.ApplyIdentityMigrationsAsync();
await app.ApplySchedulingMigrationsAsync();
await app.SeedSchedulingHolidaysAsync();

app.Run();
