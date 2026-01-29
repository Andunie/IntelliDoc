using IntelliDoc.Modules.Integration.Data;
using IntelliDoc.Modules.Integration.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelliDoc.Modules.Integration;

public static class IntegrationModuleExtensions
{
    public static IServiceCollection AddIntegrationModule(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Veritabanı Bağlantısı
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<IntegrationDbContext>(options =>
            options.UseNpgsql(connectionString));

        // 2. Servisleri Kaydet
        services.AddScoped<WebhookSender>();

        return services;
    }
}
