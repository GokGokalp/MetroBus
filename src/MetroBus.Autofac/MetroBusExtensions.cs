using Autofac;
using GreenPipes;
using MassTransit;
using MassTransit.RabbitMqTransport;
using System;

namespace MetroBus.Autofac
{
    public static class MetroBusExtensions
    {
        public static MetroBusInitializer RegisterConsumer<TConsumer>(this MetroBusInitializer instance, string queueName, ILifetimeScope lifetimeScope) where TConsumer : class, IConsumer
        {
            Action<IRabbitMqBusFactoryConfigurator, IRabbitMqHost> action = (cfg, host) =>
            {
                if (string.IsNullOrEmpty(queueName))
                {
                    cfg.ReceiveEndpoint(host, ConfigureReceiveEndpoint<TConsumer>(instance, lifetimeScope));
                }
                else
                {
                    cfg.ReceiveEndpoint(host, queueName, ConfigureReceiveEndpoint<TConsumer>(instance, lifetimeScope));
                }
            };

            instance.MetroBusConfiguration.BeforeBuildActions.Add(action);

            return instance;
        }

        private static Action<IRabbitMqReceiveEndpointConfigurator> ConfigureReceiveEndpoint<TConsumer>(MetroBusInitializer instance, ILifetimeScope lifetimeScope) where TConsumer : class, IConsumer
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

                _.Consumer<TConsumer>(lifetimeScope);
            };
        }
    }
}