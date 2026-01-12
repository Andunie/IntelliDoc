using IntelliDoc.Modules.Intake.Data;
using IntelliDoc.Modules.Intake.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IntelliDoc.Modules.Intake;

public static class IntakeModuleExtensions
{
    public static IServiceCollection AddIntakeModule(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Veritabanı Bağlantısı
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<IntakeDbContext>(options =>
            options.UseNpgsql(connectionString));

        // 2. Servisleri Kaydet
        services.AddScoped<MinioStorageService>();

        return services;
    }
}