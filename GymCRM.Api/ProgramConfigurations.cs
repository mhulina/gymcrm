using Asp.Versioning;
using GymCRM.BillingAPI;
using GymCRM.IdentityAPI;
using GymCRM.SchedulingAPI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text.Json.Nodes;

namespace GymCRM.Api;

/// <summary>
/// Cross-cutting setup that must exist exactly once per process (auth, CORS, versioning,
/// Swagger, rate limiting). Module-specific wiring lives in each module's own project
/// (see <see cref="IdentityModule"/> and <see cref="SchedulingModule"/>).
/// </summary>
public static class ProgramConfigurations
{
    public static IServiceCollection SetupCors(this IServiceCollection services, IConfiguration config)
    {
        var configuredOrigins = config["Cors:AllowedOrigins"]
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var origins = configuredOrigins is { Length: > 0 }
            ? configuredOrigins
            : new[] { "http://localhost:3000", "http://localhost:55080", "http://localhost:55085" };

        services.AddCors(opt =>
            opt.AddPolicy(
                name: "AllowAny",
                policy => policy
                    .WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()));

        return services;
    }

    public static IServiceCollection SetupAuthentication(
        this IServiceCollection services,
        IConfiguration config,
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

                opt.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Cookies["accessToken"];

                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };

                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["Authentication:Issuer"],
                    ValidAudience = config["Authentication:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Convert.FromBase64String(secretForKey))
                };
            });

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
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "GymCRM API", Version = "v1.0" });
            c.SwaggerDoc("v2", new OpenApiInfo { Title = "GymCRM API", Version = "v2.0" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 1safsfsdfdfd\"",
            });
            c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                { new OpenApiSecuritySchemeReference("Bearer", document), new List<string>() }
            });

            foreach (var assembly in new[] { typeof(IdentityModule).Assembly, typeof(SchedulingModule).Assembly, typeof(BillingModule).Assembly })
            {
                var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");

                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            }

            c.MapType<TimeOnly>(() => new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = "HH:mm",
                Example = JsonValue.Create("09:30")
            });

            c.MapType<DateOnly>(() => new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = "yyyy-MM-dd",
                Example = JsonValue.Create("2025-01-01")
            });
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
