using System;

namespace MetroBus.Microsoft.Extensions.DependencyInjection
{
    public static class MetroBusExtensions
    {
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