using IntelliDoc.Modules.Search.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace IntelliDoc.Modules.Search;

public static class SearchModuleExtensions
{
    public static IServiceCollection AddSearchModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<SearchService>();
        return services;
    }
}