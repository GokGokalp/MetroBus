using System;
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

        private IBusControl _bus;

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

        public MetroBusInitializer UseRabbitMq(string rabbitMqUri, string rabbitMqUserName,
            string rabbitMqPassword)
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

        public MetroBusInitializer InitializeConsumer<TConsumer>(string queueName) where TConsumer : class, IConsumer, new()
        {
            _bus = CreateBusForConsumer<TConsumer>(queueName);

            return this;
        }

        public MetroBusInitializer InitializeConsumer<TConsumer>(string queueName, Func<TConsumer> consumer) where TConsumer : class, IConsumer, new()
        {
            _bus = _bus = CreateBusForConsumer(queueName, consumer);

            return this;
        }

        public async Task<ISendEndpoint> InitializeProducer(string queueName)
        {
            _bus = CreateBus();

            if (!_rabbitMqUri.EndsWith("/"))
            {
                queueName = queueName.Insert(0, "/");
            }

            var sendToUri = new Uri($"{_rabbitMqUri}{queueName}");

            return await _bus.GetSendEndpoint(sendToUri);
        }

        public IRequestClient<TRequest, TResponse> InitializeRequestClient<TRequest, TResponse>(string address,
            int? requestTimeoutFromSeconds = null) where TRequest : class
                                                   where TResponse : class
        {
            IBusControl bus = CreateBus();
            var serviceAddress = new Uri($"loopback://{address}");

            return new MessageRequestClient<TRequest, TResponse>(bus,
                                                                 serviceAddress,
                                                                 TimeSpan.FromSeconds(requestTimeoutFromSeconds ?? _defaultRequestTimeoutFromSeconds));
        }

        public IBusControl Build()
        {
            if (_bus == null)
            {
                _bus = CreateBus();
            }

            return _bus;
        }

        public async Task<BusHandle> Start()
        {
            return await _bus.StartAsync();
        }
        #endregion

        #region Private Methods

        private IBusControl CreateBus()
        {
            _bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                var host = cfg.Host(new Uri(_rabbitMqUri), hst =>
                {
                    hst.Username(_rabbitMqUserName);
                    hst.Password(_rabbitMqPassword);
                });
            });

            return _bus;
        }

        private IBusControl CreateBusForConsumer<TConsumer>(string queueName, Func<TConsumer> consumer = null) where TConsumer : class, IConsumer, new()
        {
            return Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                var host = cfg.Host(new Uri(_rabbitMqUri), hst =>
                {
                    hst.Username(_rabbitMqUserName);
                    hst.Password(_rabbitMqPassword);
                });

                UseCircuitBreaker(cfg);
                UseRateLimiter(cfg);
                UseIncrementalRetryPolicy(cfg);
                UseMessageScheduler(cfg);

                cfg.ReceiveEndpoint(host, queueName, e =>
                {
                    if (consumer != null)
                    {
                        e.Consumer(consumer);
                    }
                    else
                    {
                        e.Consumer<TConsumer>();
                    }
                });
            });
        }

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

                cfg.UseMessageScheduler(new Uri(quartzEndpoint));
            }
        }
        #endregion
    }
}