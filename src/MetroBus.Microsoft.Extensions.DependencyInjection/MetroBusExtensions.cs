using System;
using GreenPipes;
using MassTransit;
using MassTransit.RabbitMqTransport;

namespace MetroBus.Microsoft.Extensions.DependencyInjection
{
    public static class MetroBusExtensions
    {
        public static MetroBusInitializer RegisterConsumer(this MetroBusInitializer instance, string queueName, IServiceProvider serviceProvider)
        {
            Action<IRabbitMqBusFactoryConfigurator, IRabbitMqHost> action = (cfg, host) =>
            {
                if (queueName == null)
                {
                    cfg.ReceiveEndpoint(host, ConfigureReceiveEndpoint(instance, serviceProvider));
                }
                else
                {
                    cfg.ReceiveEndpoint(host, queueName, ConfigureReceiveEndpoint(instance, serviceProvider));
                }
            };

            instance.MetroBusConfiguration.BeforeBuildActions.Add(action);

            return instance;
        }

        private static Action<IRabbitMqReceiveEndpointConfigurator> ConfigureReceiveEndpoint(MetroBusInitializer instance, IServiceProvider serviceProvider)
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

                _.LoadFrom(serviceProvider);
            };
        }
    }
}