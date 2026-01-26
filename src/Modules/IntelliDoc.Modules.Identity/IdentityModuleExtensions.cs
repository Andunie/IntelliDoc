using IntelliDoc.Modules.Identity.Data;
using IntelliDoc.Modules.Identity.Entities;
using IntelliDoc.Modules.Identity.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace IntelliDoc.Modules.Identity;

public static class IdentityModuleExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration config)
    {
        // 1. DbContext
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("DefaultConnection")));

        // 2. Identity Core
        services.AddIdentityCore<ApplicationUser>(opt =>
        {
            opt.Password.RequireNonAlphanumeric = false; // Basit şifre (Dev için)
            opt.Password.RequireDigit = false;
            opt.Password.RequireUppercase = false;
        })
        .AddEntityFrameworkStores<IdentityDbContext>()
        .AddDefaultTokenProviders();

        // 3. JWT Authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["Jwt:Issuer"] ?? "IntelliDoc",
                    ValidAudience = config["Jwt:Audience"] ?? "IntelliDoc",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"] ?? "SuperSecretKey12345678901234567890"))
                };
            });

        // 4. Servisler
        services.AddScoped<TokenService>();
        services.AddScoped<EmailService>();

        return services;
    }
}