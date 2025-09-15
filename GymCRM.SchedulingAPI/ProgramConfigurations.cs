using System.Reflection;
using Asp.Versioning;
using GymCRM.SchedulingAPI.Infrastructure.Implementation;
using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models.DTOs;
using GymCRM.SchedulingAPI.Services;
using GymCRM.SchedulingAPI.Services.Implementation;
using GymCRM.SchedulingAPI.Services.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Holiday = GymCRM.SchedulingAPI.Models.Entities.Holiday;

namespace GymCRM.SchedulingAPI;

public static class ProgramConfigurations
{
    public static IServiceCollection AddProjectServices(this IServiceCollection services)
    {
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<ITrainingSessionsRepository, TrainingSessionsRepository>();
        services.AddScoped<ITrainingSessionsService, TrainingSessionsService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAvailabilitiesRepository, AvailabilitiesRepository>();
        services.AddScoped<IAvailabilitiesService, AvailabilitiesService>();
        services.AddScoped<ITimeOffRepository, TimeOffRepository>();
        services.AddScoped<ITimeOffService, TimeOffService>();
        services.AddScoped<IHolidayRepository, HolidayRepository>();
        services.AddScoped<IHolidayService, HolidayService>();
        services.AddHttpClient<HolidaySeeder>();
        
        return services;
    }
    
    public static IServiceCollection AutoMapper(this IServiceCollection services)
    {
        services.AddAutoMapper(config =>
        {
            config.CreateMap<TrainingSession, GymCRM.SchedulingAPI.Models.Entities.TrainingSession>();
            config.CreateMap<GymCRM.SchedulingAPI.Models.Entities.TrainingSession, TrainingSession>();
            config.CreateMap<Availability, GymCRM.SchedulingAPI.Models.Entities.Availability>();
            config.CreateMap<GymCRM.SchedulingAPI.Models.Entities.Availability, Availability>();
            config.CreateMap<GymCRM.SchedulingAPI.Models.Entities.TimeOff, TimeOff>();
            config.CreateMap<TimeOff, GymCRM.SchedulingAPI.Models.Entities.TimeOff>();
            config.CreateMap<Holiday, Models.DTOs.Holiday>();
            config.CreateMap<Models.DTOs.Holiday, Holiday>();
        });

        return services;
    }

    public static IServiceCollection Cors(this IServiceCollection services)
    {
        services.AddCors(opt =>
            opt.AddPolicy(
                name: "AllowAny", 
                policy => policy
                    .WithOrigins("http://localhost:3000", "http://localhost:55085")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()));
        
        return services;
    }

    public static IServiceCollection Authentication(
        this IServiceCollection services, 
        WebApplicationBuilder builder, 
        string secretForKey)
    {
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
                opt.TokenValidationParameters = new TokenValidationParameters
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

        return services;
    }

    public static IServiceCollection ApiVersioning(this IServiceCollection services)
    {
        services
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

        return services;
    }

    public static IServiceCollection SwaggerGen(this IServiceCollection services)
    {
        services.AddSwaggerGen(opt =>
        {
            opt.SwaggerDoc("v1", new OpenApiInfo { Title = "SchedulingAPI", Version = "v1.0" });
            opt.SwaggerDoc("v2", new OpenApiInfo { Title = "SchedulingAPI", Version = "v2.0" });
            opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 1safsfsdfdfd\"",
            });
            opt.AddSecurityRequirement(new OpenApiSecurityRequirement
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
	    
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            opt.IncludeXmlComments(xmlPath);
        });
        
        return services;
    }
}