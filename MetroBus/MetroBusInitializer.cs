using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using MassTransit.RabbitMqTransport;

namespace MetroBus
{
    public class MetroBusInitializer
    {
        private static readonly Lazy<MetroBusInitializer> _Instance = new Lazy<MetroBusInitializer>(() => new MetroBusInitializer());

        private string _rabbitMqUri;
        private string _rabbitMqUserName;
        private string _rabbitMqPassword;

        private int? _tripThreshold;
        private int? _activeThreshold;
        private int? _resetInterval;

        private int? _rateLimit;
        private int? _rateLimiterInterval;

        private Exception[] _retryOnSpecificExceptionTypes;
        private int? _retryLimit;
        private int? _initialRetryIntervalFromMinute;
        private int? _intervalRetryIncrementFromMinute;

        private readonly int _defaultRequestTimeoutFromSeconds;

        private bool _useMessageScheduler;
        private bool _useDelayedExchangeMessageScheduler;

        private IBusControl _bus;
        private List<Action<IRabbitMqBusFactoryConfigurator, IRabbitMqHost>> _beforeBuildActions = new List<Action<IRabbitMqBusFactoryConfigurator, IRabbitMqHost>>();

        private MetroBusInitializer()
        {
            _defaultRequestTimeoutFromSeconds = 20;
        }

        public static MetroBusInitializer Instance => _Instance.Value;

        #region Fluent Methods
        public MetroBusInitializer UseCircuitBreaker(int tripThreshold, int activeThreshold, int resetInterval)
        {
            _tripThreshold = tripThreshold;
            _activeThreshold = activeThreshold;
            _resetInterval = resetInterval;

            return this;
        }

        public MetroBusInitializer UseRateLimiter(int rateLimit, int interval)
        {
            _rateLimit = rateLimit;
            _rateLimiterInterval = interval;

            return this;
        }

        public MetroBusInitializer UseIncrementalRetryPolicy(int retryLimit, int initialIntervalFromMinute, int intervalIncrementFromMinute, params Exception[] retryOnSpecificExceptionType)
        {
            _retryOnSpecificExceptionTypes = retryOnSpecificExceptionType;
            _retryLimit = retryLimit;
            _initialRetryIntervalFromMinute = initialIntervalFromMinute;
            _intervalRetryIncrementFromMinute = intervalIncrementFromMinute;

            return this;
        }

        public MetroBusInitializer UseRabbitMq(string rabbitMqUri, string rabbitMqUserName, string rabbitMqPassword)
        {
            _rabbitMqUri = rabbitMqUri;
            _rabbitMqUserName = rabbitMqUserName;
            _rabbitMqPassword = rabbitMqPassword;

            return this;
        }

        public MetroBusInitializer UseMessageScheduler()
        {
            _useMessageScheduler = true;
            return this;
        }

        public MetroBusInitializer UseDelayedExchangeMessageScheduler()
        {
            _useDelayedExchangeMessageScheduler = true;
            return this;
        }

        public MetroBusInitializer RegisterConsumer<TConsumer>(string queueName, Func<TConsumer> resolveFunction = null) where TConsumer : class, IConsumer, new()
        {
            Action<IRabbitMqBusFactoryConfigurator, IRabbitMqHost> action = (cfg, host) =>
            {
                cfg.ReceiveEndpoint(host, queueName, e =>
                {
                    if (resolveFunction != null)
                    {
                        e.Consumer(resolveFunction);
                    }
                    else
                    {
                        e.Consumer<TConsumer>();
                    }
                });
            };

            _beforeBuildActions.Add(action);

            return this;
        }

        public async Task<ISendEndpoint> InitializeProducer(string queueName)
        {
            _bus = Build();

            if (!_rabbitMqUri.EndsWith("/"))
            {
                queueName = queueName.Insert(0, "/");
            }

            var sendToUri = new Uri($"{_rabbitMqUri}{queueName}");

            return await _bus.GetSendEndpoint(sendToUri);
        }

        public IRequestClient<TRequest, TResponse> InitializeRequestClient<TRequest, TResponse>(string address, int? requestTimeoutFromSeconds = null) where TRequest : class
                                                   where TResponse : class
        {
            IBusControl bus = Build();
            var serviceAddress = new Uri($"loopback://{address}");

            return new MessageRequestClient<TRequest, TResponse>(bus,
                                                                 serviceAddress,
                                                                 TimeSpan.FromSeconds(requestTimeoutFromSeconds ?? _defaultRequestTimeoutFromSeconds));
        }

        public IBusControl Build()
        {
            return Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                var host = cfg.Host(new Uri(_rabbitMqUri), hst =>
                {
                    hst.Username(_rabbitMqUserName);
                    hst.Password(_rabbitMqPassword);
                });

                foreach (Action<IRabbitMqBusFactoryConfigurator, IRabbitMqHost> action in _beforeBuildActions)
                {
                    action.Invoke(cfg, host);
                }

                UseCircuitBreaker(cfg);
                UseRateLimiter(cfg);
                UseIncrementalRetryPolicy(cfg);
                UseMessageScheduler(cfg);
                UseDelayedExchangeMessageScheduler(cfg);
            });
        }

        public async Task<BusHandle> Start()
        {
            return await _bus.StartAsync();
        }
        #endregion

        #region Private Methods

        private void UseIncrementalRetryPolicy(IRabbitMqBusFactoryConfigurator cfg)
        {
            if (_retryLimit != null && _initialRetryIntervalFromMinute != null && _intervalRetryIncrementFromMinute != null)
            {
                cfg.UseRetry(retryConfig =>
                {
                    if (_retryOnSpecificExceptionTypes != null)
                    {
                        foreach (var exception in _retryOnSpecificExceptionTypes)
                        {
                            retryConfig.Handle(exception.GetType());
                        }
                    }

                    retryConfig.Incremental(_retryLimit.Value, TimeSpan.FromMinutes(_initialRetryIntervalFromMinute.Value), TimeSpan.FromMinutes(_intervalRetryIncrementFromMinute.Value));
                });
            }
        }

        private void UseCircuitBreaker(IRabbitMqBusFactoryConfigurator cfg)
        {
            if (_tripThreshold != null && _activeThreshold != null && _resetInterval != null)
            {
                cfg.UseCircuitBreaker(cb =>
                {
                    cb.TripThreshold = _tripThreshold.Value;
                    cb.ActiveThreshold = _activeThreshold.Value;
                    cb.ResetInterval = TimeSpan.FromMinutes(_resetInterval.Value);
                });
            }
        }

        private void UseRateLimiter(IRabbitMqBusFactoryConfigurator cfg)
        {
            if (_rateLimit != null && _rateLimiterInterval != null)
            {
                cfg.UseRateLimit(_rateLimit.Value, TimeSpan.FromSeconds(_rateLimiterInterval.Value));
            }
        }

        private void UseMessageScheduler(IRabbitMqBusFactoryConfigurator cfg)
        {
            if (_useMessageScheduler)
            {
                string quartzEndpoint = _rabbitMqUri;
                if (!quartzEndpoint.EndsWith("/"))
                {
                    quartzEndpoint = quartzEndpoint.Insert(0, "/");
                }

                cfg.UseMessageScheduler(new Uri(string.Concat(quartzEndpoint, "quartz")));
            }
        }

        private void UseDelayedExchangeMessageScheduler(IRabbitMqBusFactoryConfigurator cfg)
        {
            if (_useDelayedExchangeMessageScheduler)
            {
                cfg.UseDelayedExchangeMessageScheduler();
            }
        }

        #endregion
    }
}