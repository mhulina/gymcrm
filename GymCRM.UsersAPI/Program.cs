using AutoMapper;
using GymCRM.UsersAPI.Infrastructure;
using GymCRM.UsersAPI.Infrastructure.Implementation;
using GymCRM.UsersAPI.Infrastructure.Interface;
using GymCRM.UsersAPI.Services;
using GymCRM.UsersAPI.Services.Implementation;
using GymCRM.UsersAPI.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

using var log = new LoggerConfiguration()
	.WriteTo.Console()
	.WriteTo.File("./logs/Users/logs.txt")
	.CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(option =>
{
	option.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

IMapper mapper = MappingConfig.RegisterMaps().CreateMapper();
builder.Services.AddSingleton(mapper);
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddSingleton<ILogger>(log);
builder.Services.AddScoped<IGymUsersRepository, GymUsersRepository>();
builder.Services.AddScoped<IGymUsersService, GymUsersService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
ApplyMigration();
app.Run();


void ApplyMigration()
{
	using (var scope = app.Services.CreateScope())
	{
		var _db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		if (!_db.Database.CanConnect())
		{
			_db.Database.Migrate();
		}
		else if (_db.Database.GetPendingMigrations().Count() > 0)
		{
			_db.Database.Migrate();
		}
	}
}
