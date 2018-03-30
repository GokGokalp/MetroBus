using Autofac;
using GreenPipes;
using MassTransit;
using MassTransit.RabbitMqTransport;
using System;

namespace MetroBus.Autofac
{
    public static class MetroBusExtensions
    {
        public static MetroBusInitializer RegisterConsumer(this MetroBusInitializer instance, string queueName, ILifetimeScope lifetimeScope)
        {
            Action<IRabbitMqBusFactoryConfigurator, IRabbitMqHost> action = (cfg, host) =>
            {
                cfg.ReceiveEndpoint(host, queueName, e =>
                {
                    if (instance.MetroBusConfiguration.UseConcurrentConsumerLimit != null)
                    {
                        e.UseConcurrencyLimit(instance.MetroBusConfiguration.UseConcurrentConsumerLimit.Value);
                    }

                    if (instance.MetroBusConfiguration.PrefetchCount != null)
                    {
                        e.PrefetchCount = instance.MetroBusConfiguration.PrefetchCount.Value;
                    }

                    e.LoadFrom(lifetimeScope);
                });
            };

            instance.MetroBusConfiguration.BeforeBuildActions.Add(action);

            return instance;
        }
    }
}