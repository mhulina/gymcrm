using GymCRM.MembershipAPI.Infrastructure;
using GymCRM.MembershipAPI.Infrastructure.Implementation;
using GymCRM.MembershipAPI.Infrastructure.Interface;
using GymCRM.MembershipAPI.Services;
using GymCRM.MembershipAPI.Services.Implementation;
using GymCRM.MembershipAPI.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

using var log = new LoggerConfiguration()
	.WriteTo.Console()
	.WriteTo.File("./logs/Members/logs.txt")
	.CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(option =>
{
	option.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

var mapper = MappingConfig.RegisterMaps().CreateMapper();
builder.Services.AddSingleton(mapper);
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddSingleton<ILogger>(log);
builder.Services.AddScoped<IMembersRepository, MembersRepository>();
builder.Services.AddScoped<IMembersService, MembersService>();

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
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		if (!db.Database.CanConnect())
		{
			db.Database.Migrate();
		}
		else if (db.Database.GetPendingMigrations().Any())
		{
			db.Database.Migrate();
		}
	}
}
