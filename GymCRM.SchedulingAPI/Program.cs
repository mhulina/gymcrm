using GymCRM.SchedulingAPI;
using GymCRM.SchedulingAPI.Infrastructure;
using Microsoft.AspNetCore.Connections;
using Microsoft.EntityFrameworkCore;
using Serilog;

using var log = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("./logs/SchedulingAPI/logs.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(logger: log);

// Add services to the container.
builder.Services.AddDbContext<SchedulingDbContext>(option =>
{
    option.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services
	.Cors()
	.AutoMapper();

var secretForKey = builder.Configuration["Authentication:SecretForKey"];

if (string.IsNullOrEmpty(secretForKey))
{
	throw new InvalidOperationException("Secret is missing from configuration");
}

builder.Services
	.Authentication(builder, secretForKey)
	.ApiVersioning()
	.AddProjectServices()
	.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services
	.AddEndpointsApiExplorer()
	.SwaggerGen()
	.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
    app.UseSwagger();
    app.UseSwaggerUI();
// }

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();
app.MapHealthChecks("/health");

app.MapControllers();
ApplyMigration();
app.Run();

return;

void ApplyMigration()
{
	using var scope = app.Services.CreateScope();
	var db = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();

	if (!db.Database.CanConnect())
	{
		throw new ConnectionAbortedException("Database connection could not be established");
	}
		
	if (db.Database.GetPendingMigrations().Any())
	{
		db.Database.Migrate();
	}
}