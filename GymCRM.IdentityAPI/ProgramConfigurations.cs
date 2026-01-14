using System.Reflection;
using Asp.Versioning;
using GymCRM.IdentityAPI.Infrastructure;
using GymCRM.IdentityAPI.Infrastructure.Implementation;
using GymCRM.IdentityAPI.Infrastructure.Interface;
using GymCRM.IdentityAPI.Models.DTOs;
using GymCRM.IdentityAPI.Models.Implementation;
using GymCRM.IdentityAPI.Models.Interface;
using GymCRM.IdentityAPI.Services.Background;
using GymCRM.IdentityAPI.Services.Implementation;
using GymCRM.IdentityAPI.Services.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace GymCRM.IdentityAPI;

public static class ProgramConfigurations
{
    public static IConfigurationManager SetupConfiguration(this IConfigurationManager config)
    {
        config
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables();
        
        return config;
    }

    public static WebApplicationBuilder SetupContext(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddDbContext<IdentityDbContext>(option =>
            {
                option.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
            });
        
        return builder;
    }

    public static IServiceCollection SetupCors(this IServiceCollection services)
    {
        services
            .AddCors(opt =>
            {
                opt.AddPolicy(
                    name: "AllowAny", 
                    policy => policy
                        .WithOrigins("http://localhost:3000", "http://localhost:55080")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
            });
        
        return services;
    }

    public static IServiceCollection SetupAutoMapper(this IServiceCollection services)
    {
        services
            .AddAutoMapper(config =>
            {
                config.CreateMap<Member, GymCRM.IdentityAPI.Models.Entities.Member>();
                config.CreateMap<GymCRM.IdentityAPI.Models.Entities.Member, Member>();
            });
        
        return services;
    }

    public static IServiceCollection SetupAuthentication(
        this IServiceCollection services, 
        WebApplicationBuilder builder,
        string? secretForKey)
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
		
                opt.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Try to get token from cookie
                        var accessToken = context.Request.Cookies["accessToken"];
                
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }
                
                        return Task.CompletedTask;
                    }
                };
		
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
        
        return services;
    }

    public static IServiceCollection SetupDependencyInjection(this IServiceCollection services)
    {
        services
            .AddScoped<IUnitOfWork, UnitOfWork>()
            .AddScoped<IMembersRepository, MembersRepository>()
            .AddScoped<IAccountsRepository, AccountsRepository>()
            .AddScoped<IMembersService, MembersService>()
            .AddScoped<IAuthenticationService, AuthenticationService>()
            .AddScoped<IRefreshTokensRepository, RefreshTokensRepository>()
            .AddScoped<IRefreshTokenService, RefreshTokenService>()
            .AddHostedService<RefreshTokenCleanupService>();
        
        return services;
    }

    public static IServiceCollection SetupApiVersioning(this IServiceCollection services)
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

    public static IServiceCollection SetupSwagger(this IServiceCollection services)
    {
        services
            .AddSwaggerGen(c =>
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
	
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
        
        return services;
    }

    public static IServiceCollection SetupRateLimiting(this IServiceCollection services)
    {
        services
            .AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("auth", opt =>
                {
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.PermitLimit = 5;
                    opt.QueueLimit = 0;
                });

                options.AddFixedWindowLimiter("register", opt =>
                {
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.PermitLimit = 10;
                    opt.QueueLimit = 0;
                });

                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.HttpContext.Response.WriteAsJsonAsync(new
                        {
                            error = "Too many requests. Please try again later."
                        },
                        cancellationToken);
                };
            });
        
        return services;
    }
}