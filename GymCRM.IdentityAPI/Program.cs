using Asp.Versioning;
using GymCRM.IdentityAPI.Infrastructure;
using GymCRM.IdentityAPI.Infrastructure.Implementation;
using GymCRM.IdentityAPI.Models.Implementation;
using GymCRM.IdentityAPI.Models.Interface;
using GymCRM.IdentityAPI.Services.Implementation;
using GymCRM.IdentityAPI.Services.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Connections;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Member = GymCRM.IdentityAPI.Models.DTOs.Member;

using var log = new LoggerConfiguration()
	.MinimumLevel.Debug()
	.WriteTo.Console()
	.WriteTo.File("./logs/IdentityAPI/logs.txt", rollingInterval: RollingInterval.Day)
	.CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(logger: log);

// Add services to the container.
builder.Services.AddDbContext<IdentityDbContext>(option =>
{
	option.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddCors(opt =>
{
	opt.AddPolicy(
		name: "AllowAny", 
		policy => policy
			.WithOrigins("http://localhost:3000", "http://localhost:55080")
			.AllowAnyHeader()
			.AllowAnyMethod()
			.AllowCredentials());
});

builder.Services.AddAutoMapper(config =>
{
	config.CreateMap<Member, GymCRM.IdentityAPI.Models.Entities.Member>();
	config.CreateMap<GymCRM.IdentityAPI.Models.Entities.Member, Member>();
});

var secretForKey = builder.Configuration["Authentication:SecretForKey"];

if (string.IsNullOrEmpty(secretForKey))
{
	throw new InvalidOperationException("Secret is missing from configuration");
}

builder.Services
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
			ValidIssuer = builder.Configuration["Authentication:Issuer"],
			ValidAudience = builder.Configuration["Authentication:Audience"],
			IssuerSigningKey = new SymmetricSecurityKey(
				Convert.FromBase64String(secretForKey))
		};
	});

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IMembersRepository, MembersRepository>();
builder.Services.AddScoped<IAccountsRepository, AccountsRepository>();
builder.Services.AddScoped<IMembersService, MembersService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

builder.Services
	.AddApiVersioning(opt =>
	{
		opt.DefaultApiVersion = new ApiVersion(1, 0);
		opt.AssumeDefaultVersionWhenUnspecified = true;
		opt.ReportApiVersions = true;
		opt.ApiVersionReader = ApiVersionReader.Combine(
			new UrlSegmentApiVersionReader(),
			new HeaderApiVersionReader("X-Api-Version")
		);
	})
	.AddApiExplorer(opt =>
	{
		opt.GroupNameFormat = "'v'VVV";
		opt.SubstituteApiVersionInUrl = true;
	});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo { Title = "IdentityAPI", Version = "v1.0" });
	c.SwaggerDoc("v2", new OpenApiInfo { Title = "IdentityAPI", Version = "v2.0" });
	c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Name = "Authorization",
		Type = SecuritySchemeType.ApiKey,
		Scheme = "Bearer",
		BearerFormat = "JWT",
		In = ParameterLocation.Header,
		Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 1safsfsdfdfd\"",
	});
	c.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "Bearer",
				}
			},
			new string[] { }
		}
	});
});
builder.Services.AddHealthChecks();

var app = builder.Build();
// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
	app.UseSwagger();
	app.UseSwaggerUI();
// }

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();
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
