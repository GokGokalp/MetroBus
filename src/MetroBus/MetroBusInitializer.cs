using GreenPipes;
using MassTransit;
using MassTransit.RabbitMqTransport;
using MassTransit.Util;
using MetroBus.Core;
using System;

namespace MetroBus
{
    public class MetroBusInitializer
    {
        private static readonly Lazy<MetroBusInitializer> _Instance = new Lazy<MetroBusInitializer>(() => new MetroBusInitializer());
        public static MetroBusInitializer Instance => _Instance.Value;

        public MetroBusConfiguration MetroBusConfiguration { get; set; }

        private RetryPolicies _retryPolicy;
        private IBusControl _bus;

        private MetroBusInitializer()
        {
            MetroBusConfiguration = new MetroBusConfiguration();
        }

        #region Fluent Methods
        public MetroBusInitializer UseCircuitBreaker(int tripThreshold, int activeThreshold, TimeSpan resetInterval)
        {
            MetroBusConfiguration.TripThresholdForCircuitBreaker = tripThreshold;
            MetroBusConfiguration.ActiveThresholdForCircuitBreaker = activeThreshold;
            MetroBusConfiguration.ResetIntervalForCircuitBreaker = resetInterval;

            return this;
        }

        public MetroBusInitializer UseRateLimiter(int rateLimit, TimeSpan interval)
        {
            MetroBusConfiguration.RateLimit = rateLimit;
            MetroBusConfiguration.RateLimitInterval = interval;

            return this;
        }

        public RetryPolicies UseRetryPolicy()
        {
            _retryPolicy = new RetryPolicies();

            return _retryPolicy;
        }

        public MetroBusInitializer UseRabbitMq(string rabbitMqUri, string rabbitMqUserName, string rabbitMqPassword)
        {
            MetroBusConfiguration.RabbitMqUri = rabbitMqUri;
            MetroBusConfiguration.RabbitMqUserName = rabbitMqUserName;
            MetroBusConfiguration.RabbitMqPassword = rabbitMqPassword;

            return this;
        }

        public MetroBusInitializer UseMessageSchedulerWithQuartz(string quartzEndpoint)
        {
            MetroBusConfiguration.UseMessageScheduler = true;
            MetroBusConfiguration.QuartzEndpoint = quartzEndpoint;
            return this;
        }

        public MetroBusInitializer UseDelayedExchangeMessageScheduler()
        {
            MetroBusConfiguration.UseDelayedExchangeMessageScheduler = true;
            return this;
        }

        public MetroBusInitializer UseConcurrentConsumerLimit(int concurrencyLimit)
        {
            MetroBusConfiguration.UseConcurrentConsumerLimit = concurrencyLimit;
            return this;
        }

        public MetroBusInitializer SetPrefetchCount(ushort prefetchCount)
        {
            MetroBusConfiguration.PrefetchCount = prefetchCount;
            return this;
        }

        public MetroBusInitializer RegisterConsumer<TConsumer>(string queueName = null) where TConsumer : class, IConsumer, new()
        {
            Action<IRabbitMqBusFactoryConfigurator, IRabbitMqHost> action = (cfg, host) =>
            {
                if (queueName == null)
                {
                    cfg.ReceiveEndpoint(host, ConfigureReceiveEndpoint<TConsumer>());
                }
                else
                {
                    cfg.ReceiveEndpoint(host, queueName, ConfigureReceiveEndpoint<TConsumer>());
                }
            };

            MetroBusConfiguration.BeforeBuildActions.Add(action);

            return this;
        }

        public MetroBusInitializer RegisterConsumer(Func<IConsumer> resolveFunction, string queueName = null)
        {
            Action<IRabbitMqBusFactoryConfigurator, IRabbitMqHost> action = (cfg, host) =>
            {
                if (queueName == null)
                {
                    cfg.ReceiveEndpoint(host, ConfigureReceiveEndpoint(resolveFunction));
                }
                else
                {
                    cfg.ReceiveEndpoint(host, queueName, ConfigureReceiveEndpoint(resolveFunction));
                }
            };

            MetroBusConfiguration.BeforeBuildActions.Add(action);

            return this;
        }

        public ISendEndpoint InitializeCommandProducer(string queueName)
        {
            _bus = Build();

            if (!MetroBusConfiguration.RabbitMqUri.EndsWith("/"))
            {
                queueName = queueName.Insert(0, "/");
            }

            var sendToUri = new Uri($"{MetroBusConfiguration.RabbitMqUri}{queueName}");

            return TaskUtil.Await(() => _bus.GetSendEndpoint(sendToUri));
        }

        public IBusControl InitializeEventProducer()
        {
            _bus = Build();

            return _bus;
        }

        public IRequestClient<TRequest, TResponse> InitializeRequestClient<TRequest, TResponse>(string address, TimeSpan? requestTimeout = null) where TRequest : class
                                           where TResponse : class
        {
            IBusControl bus = Build();
            var serviceAddress = new Uri($"loopback://{address}");

            return new MessageRequestClient<TRequest, TResponse>(bus,
                                                                 serviceAddress,
                                                                 requestTimeout ?? MetroBusConfiguration.DefaultRequestTimeoutTime);
        }

        public IBusControl Build()
        {
            return Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                var host = cfg.Host(new Uri(MetroBusConfiguration.RabbitMqUri), hst =>
                {
                    hst.Username(MetroBusConfiguration.RabbitMqUserName);
                    hst.Password(MetroBusConfiguration.RabbitMqPassword);
                });

                foreach (Action<IRabbitMqBusFactoryConfigurator, IRabbitMqHost> action in MetroBusConfiguration.BeforeBuildActions)
                {
                    action.Invoke(cfg, host);
                }

                UseCircuitBreaker(cfg);
                UseRateLimiter(cfg);
                UseIncrementalRetryPolicy(cfg);
                UseMessageSchedulerWithQuartz(cfg, MetroBusConfiguration.QuartzEndpoint);
                UseDelayedExchangeMessageScheduler(cfg);
            });
        }

        public BusHandle Start()
        {
            return TaskUtil.Await(() => _bus.StartAsync());
        }
        #endregion

        #region Private Methods
        private Action<IRabbitMqReceiveEndpointConfigurator> ConfigureReceiveEndpoint<TConsumer>() where TConsumer : class, IConsumer, new()
        {
            return _ =>
            {
                if (MetroBusConfiguration.UseConcurrentConsumerLimit != null)
                {
                    _.UseConcurrencyLimit(MetroBusConfiguration.UseConcurrentConsumerLimit.Value);
                }

                if (MetroBusConfiguration.PrefetchCount != null)
                {
                    _.PrefetchCount = MetroBusConfiguration.PrefetchCount.Value;
                }

                _.Consumer<TConsumer>();
            };
        }

        private Action<IRabbitMqReceiveEndpointConfigurator> ConfigureReceiveEndpoint(Func<IConsumer> resolveFunction = null)
        {
            return _ =>
            {
                if (MetroBusConfiguration.UseConcurrentConsumerLimit != null)
                {
                    _.UseConcurrencyLimit(MetroBusConfiguration.UseConcurrentConsumerLimit.Value);
                }

                if (MetroBusConfiguration.PrefetchCount != null)
                {
                    _.PrefetchCount = MetroBusConfiguration.PrefetchCount.Value;
                }

                _.Consumer(resolveFunction);
            };
        }


        private void UseIncrementalRetryPolicy(IRabbitMqBusFactoryConfigurator cfg)
        {
            if (_retryPolicy != null && _retryPolicy.RetryLimit != null && _retryPolicy.InitialRetryIntervalTime != null && _retryPolicy.IntervalRetryIncrementTime != null)
            {
                cfg.UseRetry(retryConfig =>
                {
                    if (_retryPolicy.RetryOnSpecificExceptionTypes != null)
                    {
                        foreach (var exception in _retryPolicy.RetryOnSpecificExceptionTypes)
                        {
                            retryConfig.Handle(exception.GetType());
                        }
                    }

                    retryConfig.Incremental(_retryPolicy.RetryLimit.Value, _retryPolicy.InitialRetryIntervalTime.Value, _retryPolicy.IntervalRetryIncrementTime.Value);
                });
            }
        }

        private void UseCircuitBreaker(IRabbitMqBusFactoryConfigurator cfg)
        {
            if (MetroBusConfiguration.TripThresholdForCircuitBreaker != null && MetroBusConfiguration.ActiveThresholdForCircuitBreaker != null && MetroBusConfiguration.ResetIntervalForCircuitBreaker != null)
            {
                cfg.UseCircuitBreaker(cb =>
                {
                    cb.TripThreshold = MetroBusConfiguration.TripThresholdForCircuitBreaker.Value;
                    cb.ActiveThreshold = MetroBusConfiguration.ActiveThresholdForCircuitBreaker.Value;
                    cb.ResetInterval = MetroBusConfiguration.ResetIntervalForCircuitBreaker.Value;
                });
            }
        }

        private void UseRateLimiter(IRabbitMqBusFactoryConfigurator cfg)
        {
            if (MetroBusConfiguration.RateLimit != null && MetroBusConfiguration.RateLimitInterval != null)
            {
                cfg.UseRateLimit(MetroBusConfiguration.RateLimit.Value, MetroBusConfiguration.RateLimitInterval.Value);
            }
        }

        private void UseMessageSchedulerWithQuartz(IRabbitMqBusFactoryConfigurator cfg, string quartzEndpoint)
        {
            if (MetroBusConfiguration.UseMessageScheduler)
            {
                cfg.UseMessageScheduler(new Uri(quartzEndpoint));
            }
        }

        private void UseDelayedExchangeMessageScheduler(IRabbitMqBusFactoryConfigurator cfg)
        {
            if (MetroBusConfiguration.UseDelayedExchangeMessageScheduler)
            {
                cfg.UseDelayedExchangeMessageScheduler();
            }
        }
        #endregion
    }
}