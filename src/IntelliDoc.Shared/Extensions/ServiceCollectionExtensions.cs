using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace IntelliDoc.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    // Assembly[] moduleAssemblies parametresini kaldırdık çünkü MediatR taraması yapmayacağız.
    // Consumer'lar için gerekirse tekrar ekleriz ama şimdilik sade tutalım.
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, IConfiguration config, params Assembly[] assemblies)
    {
        // MassTransit (RabbitMQ) Kurulumu
        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            x.AddConsumers(assemblies);

            // Not: İleride modüller Consumer ekledikçe buraya "x.AddConsumers..." ekleyeceğiz.

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqHost = config["RabbitMq:Host"] ?? "localhost";

                cfg.Host(rabbitMqHost, "/", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}