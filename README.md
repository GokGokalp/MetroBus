#   **MetroBus**
------------------------------

![alt tag](https://raw.githubusercontent.com/GokGokalp/MetroBus/master/misc/metrobus-logo.png)

Lightweight messaging wrapper of _MassTransit_

[![NuGet version](https://badge.fury.io/nu/MetroBus.svg)](https://badge.fury.io/nu/MetroBus)
[![NuGet version](https://badge.fury.io/nu/MetroBus.Core.svg)](https://badge.fury.io/nu/MetroBus.Core)
[![NuGet version](https://badge.fury.io/nu/MetroBus.Autofac.svg)](https://badge.fury.io/nu/MetroBus.Autofac)
[![NuGet version](https://badge.fury.io/nu/MetroBus.Microsoft.Extensions.DependencyInjection.svg)](https://badge.fury.io/nu/MetroBus.Microsoft.Extensions.DependencyInjection)

### NuGet Packages
``` 
> dotnet add package MetroBus
> dotnet add package MetroBus.Autofac
> dotnet add package MetroBus.Microsoft.Extensions.DependencyInjection
```

#### Supports:
- .NET Standard 2.0

#### Features:
- Currently only supports RabbitMQ transport
- Provides easy way to create **Producer** and **Consumer** for Pub/Sub
- Provides easy way to handle **Request/Response** conversations
- Provides message scheduling
- Includes optional incremental auto retry policy
- Includes optional circuit breaker
- Includes optional rate limiter
- Autofac support
- Microsoft.Extensions.DependencyInjection support

Usage:
-----

Initializing bus instance for **Producer**:

```cs
// For events
IBusControl bus = MetroBusInitializer.Instance.UseRabbitMq(rabbitMqUri, rabbitMqUserName, rabbitMqPassword)
					.InitializeEventProducer();

// For commands
ISendEndpoint bus = MetroBusInitializer.Instance.UseRabbitMq(rabbitMqUri, rabbitMqUserName, rabbitMqPassword)
                    .InitializeCommandProducer(queueName);
```

after bus instance initializing then you can use _Send_ or _Publish_ methods.

```cs
// For events
await bus.Publish<TEvent>(new
{
    SomeProperty = SomeValue
}));

// For commands
await bus.Send<TCommand>(new
{
    SomeProperty = SomeValue
}));
```


using **Consumer**:

```cs
static void Main(string[] args)
{
	IBusControl bus = MetroBusInitializer.Instance
                        .UseRabbitMq(string rabbitMqUri, string rabbitMqUserName, string rabbitMqPassword)
                        .RegisterConsumer<TCommandConsumer>(string queueName)
                        .RegisterConsumer<TEventConsumer>(string queueName)
                        .Build();

	bus.Start();

	//if you want to stop
	bus.Stop();

	Console.ReadLine();
}
```


_TCommandConsumer_ could like below:

```cs
public class TCommandConsumer : IConsumer<TCommand>
{
    public async Task Consume(ConsumeContext<TCommand> context)
    {
        var command = context.Message;

		//do something...
        await Console.Out.WriteAsync($"{command.SomeProperty}");
    }
}
```

Initializing bus instance for Request/Response conversation:

```cs
IRequestClient<TRequest, TResponse> client = MetroBusInitializer.Instance.UseRabbitMq(string rabbitMqUri, string rabbitMqUserName, string rabbitMqPassword)
                                                                    .InitializeRequestClient<TRequest, TResponse>(string queueName);

TResponse result = await client.Request(new TRequest
{
    Command = "Say hello!"
});
```

and consumer for Request/Response conversation could like below:

```cs
public class TCommandConsumer : IConsumer<TRequest>
{
    public async Task Consume(ConsumeContext<TRequest> context)
    {
        var command = context.Message;

		//do something...
        await Console.Out.WriteAsync($"{command.SomeProperty}");

		//and
		context.Respond(new TRequest
            {
                Command = "Hello!"
            });
    }
}
```

using **Consumer** with Microsoft.Extensions.DependencyInjection:
```cs
new HostBuilder ()
    .ConfigureServices ((hostContext, services) =>
    {
        services.AddMetroBus (x =>
        {
            x.AddConsumer<TCommandConsumer>();
            x.AddConsumer<TEventConsumer>();
        });

        services.AddSingleton<IBusControl> (provider => MetroBusInitializer.Instance
                .UseRabbitMq (string rabbitMqUri, string rabbitMqUserName, string rabbitMqPassword)
                .RegisterConsumer<TCommandConsumer>("foo.command.queue", provider)
                .RegisterConsumer<TEventConsumer>("foo.event.queue", provider)
                .Build ())
            .BuildServiceProvider ();

        services.AddHostedService<BusService> ();
    })
    .RunConsoleAsync ().Wait ();


public class BusService : IHostedService
{
    private readonly IBusControl _busControl;

    public BusService(IBusControl busControl)
    {
        _busControl = busControl;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _busControl.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _busControl.StopAsync(cancellationToken);
    }
}
```

**PS**: **Publisher** and **Consumer** services must be used same _TCommand_ or _TEvent_ interfaces. This is important for MassTransit integration. Also one other thing is _rabbitMqUri_ parameter must start with "rabbitmq://" prefix.


There are several options you can set via fluent interface:
- `.UseRetryPolicy().UseIncrementalRetryPolicy(int retryLimit, TimeSpan? initialIntervalTime, TimeSpan? intervalIncrementTime, params Exception[] retryOnSpecificExceptionType).Then()`
- `.UseCircuitBreaker(int tripThreshold, int activeThreshold, TimeSpan? resetInterval)`
- `.UseRateLimiter(int rateLimit, TimeSpan? interval)`
- `.UseMessageScheduler()`
- `.UseDelayedExchangeMessageScheduler()`
- `.UseConcurrentConsumerLimit(int concurrencyLimit)`