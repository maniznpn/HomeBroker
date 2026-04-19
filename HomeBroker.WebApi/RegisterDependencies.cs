using HomeBroker.Application;
using HomeBroker.Application.Common;
using HomeBroker.Application.IImageService;
using HomeBroker.Application.IServiceInterfaces.ICommissionService;
using HomeBroker.Application.IServiceInterfaces.IPropertyListingService;
using HomeBroker.Application.IServiceInterfaces.IUserMeta;
using HomeBroker.Application.IUnitOfWork;
using HomeBroker.Application.ServiceInterfaces;
using HomeBroker.Domain.IRepositories;
using HomeBroker.Infrastructure;
using HomeBroker.Infrastructure.DbContext;
using HomeBroker.Infrastructure.Identity;
using HomeBroker.Infrastructure.Services;
using HomeBroker.Infrastructure.Services.CommissionService;
using HomeBroker.Infrastructure.Services.ImageService;
using HomeBroker.Infrastructure.Services.PropertyListingService;
using HomeBroker.Infrastructure.Services.UserMeta;
using HomeBroker.WebApi.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using System.Text;

namespace HomeBroker.WebApi
{
    public static class RegisterDependencies
    {
        public static void RegisterServices(this IServiceCollection services, IConfiguration configuration)
        {
            // *** IMPORTANT: Registration order matters! ***

            // 1. Register DbContext FIRST
            services.AddDbContext<HomeBrokerDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // 2. Register Repositories (they depend on DbContext)
            services.AddScoped<IPropertyListingRepository, PropertyListingRepository>();
            services.AddScoped<ICommissionConfigurationRepository, CommissionConfigurationRepository>();

            // 3. Register UnitOfWork (it depends on repositories and DbContext)
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // 4. Register Application Services (they depend on UnitOfWork)
            services.AddScoped<IPropertyListingService, PropertyListingService>();
            services.AddScoped<ICommissionService, CommissionService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IUserMeta, UserMeta>();
            services.AddScoped<IImageService, ImageService>();

            // 5. Register other infrastructure
            services.AddHttpContextAccessor();
            services.AddScoped<IAuthorizationHandler, RoleAuthorizationHandler>();
            services.AddMemoryCache();

            // CORS Configuration
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                    builder.AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials()
                           .SetIsOriginAllowed((hosts) => true));
            });

            // Identity Configuration
            services.AddIdentity<ApplicationUser, IdentityRole<long>>(config =>
            {
                config.Password.RequiredLength = 8;
                config.Password.RequireUppercase = true;
                config.Password.RequireLowercase = true;
                config.Password.RequiredUniqueChars = 1;
                config.Password.RequireDigit = true;

                config.SignIn.RequireConfirmedEmail = false;
                config.User.RequireUniqueEmail = false;
            })
            .AddEntityFrameworkStores<HomeBrokerDbContext>()
            .AddDefaultTokenProviders();

            // Configure JWT
            services.Configure<JwtConfiguration>(options =>
            {
                var jwtConfig = configuration.GetSection("JwtConfiguration").Get<JwtConfiguration>();
                options.JwtSecurityKey = jwtConfig?.JwtSecurityKey ?? "";
                options.Issuer = jwtConfig?.Issuer ?? "";
                options.Audience = jwtConfig?.Audience ?? "";
            });

            services.Configure<CloudinarySettings>(options =>
            {
                var cloudinaryConfig = configuration.GetSection("CloudinarySettings").Get<CloudinarySettings>();
                options.ApiKey = cloudinaryConfig.ApiKey;
                options.ApiSecret = cloudinaryConfig.ApiSecret;
                options.CloudName = cloudinaryConfig.CloudName;
            });

            // Add Controllers
            services.AddControllers();

            // Add Endpoints API Explorer
            services.AddEndpointsApiExplorer();

            // Add Swagger Gen
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "HomeBroker API",
                    Version = "v1",
                    Description = "API documentation for HomeBroker Application"
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("HouseSeekerPolicy", policy =>
                    policy.Requirements.Add(new RoleRequirement(nameof(UserRole.HouseSeeker))));

                options.AddPolicy("BrokerPolicy", policy =>
                    policy.Requirements.Add(new RoleRequirement(nameof(UserRole.Broker))));

                options.AddPolicy("AdminPolicy", policy =>
                   policy.Requirements.Add(new RoleRequirement(nameof(UserRole.Admin))));
            });
        }

        public static void ConfigureAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtConfig = configuration.GetSection("JwtConfiguration").Get<JwtConfiguration>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtConfig?.Issuer,
                    ValidAudience = jwtConfig?.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig?.JwtSecurityKey ?? ""))
                };
            });
        }
    }
}
