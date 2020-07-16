using System;
using GreenPipes;
using MassTransit;
using MassTransit.RabbitMqTransport;
using Microsoft.Extensions.DependencyInjection;
using MassTransit.ExtensionsDependencyInjectionIntegration;

namespace MetroBus.Microsoft.Extensions.DependencyInjection
{
    public static class MetroBusExtensions
    {
        public static IServiceCollection AddMetroBus(this IServiceCollection serviceCollection, Action<IServiceCollectionConfigurator> configure = null)
        {
            serviceCollection.AddMassTransit(configure);

            return serviceCollection;
        }

        public static MetroBusInitializer RegisterConsumer<TConsumer>(this MetroBusInitializer instance, string queueName, IServiceProvider serviceProvider) where TConsumer : class, IConsumer
        {
            Action<IRabbitMqBusFactoryConfigurator, IRabbitMqHost> action = (cfg, host) =>
            {
                if (string.IsNullOrEmpty(queueName))
                {
                    cfg.ReceiveEndpoint(host, ConfigureReceiveEndpoint<TConsumer>(instance, serviceProvider));
                }
                else
                {
                    cfg.ReceiveEndpoint(host, queueName, ConfigureReceiveEndpoint<TConsumer>(instance, serviceProvider));
                }
            };

            instance.MetroBusConfiguration.BeforeBuildActions.Add(action);

            return instance;
        }

        private static Action<IRabbitMqReceiveEndpointConfigurator> ConfigureReceiveEndpoint<TConsumer>(MetroBusInitializer instance, IServiceProvider serviceProvider) where TConsumer : class, IConsumer
        {
            return _ =>
            {
                if (instance.MetroBusConfiguration.UseConcurrentConsumerLimit != null)
                {
                    _.UseConcurrencyLimit(instance.MetroBusConfiguration.UseConcurrentConsumerLimit.Value);
                }

                if (instance.MetroBusConfiguration.PrefetchCount != null)
                {
                    _.PrefetchCount = instance.MetroBusConfiguration.PrefetchCount.Value;
                }

                _.Consumer<TConsumer>(serviceProvider);
            };
        }
    }
}