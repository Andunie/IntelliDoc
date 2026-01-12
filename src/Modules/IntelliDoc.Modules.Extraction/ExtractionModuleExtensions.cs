using IntelliDoc.Modules.Extraction.Consumers;
using IntelliDoc.Modules.Extraction.Data;
using IntelliDoc.Modules.Extraction.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;

namespace IntelliDoc.Modules.Extraction;

public static class ExtractionModuleExtensions
{
    public static IServiceCollection AddExtractionModule(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Veritabanı
        services.AddDbContext<ExtractionDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // 2. HTTP Client (AI Service için)
        services.AddHttpClient<AiExtractionService>();

        // 3. MinIO Client (Burada da lazım çünkü Intake modülüne bağımlı değiliz)
        services.AddMinio(configureClient => configureClient
            .WithEndpoint("localhost", 9000)
            .WithCredentials("minioadmin", "minioadmin")
            .WithSSL(false) // <--- KRİTİK NOKTA: SSL'i zorla kapat
            .Build());

        return services;
    }
}