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
                if (queueName == null)
                {
                    cfg.ReceiveEndpoint(host, ConfigureReceiveEndpoint(instance, lifetimeScope));
                }
                else
                {
                    cfg.ReceiveEndpoint(host, queueName, ConfigureReceiveEndpoint(instance, lifetimeScope));
                }
            };

            instance.MetroBusConfiguration.BeforeBuildActions.Add(action);

            return instance;
        }

        private static Action<IRabbitMqReceiveEndpointConfigurator> ConfigureReceiveEndpoint(MetroBusInitializer instance, ILifetimeScope lifetimeScope)
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

                _.LoadFrom(lifetimeScope);
            };
        }
    }
}