#   **MetroBus**
------------------------------

![alt tag](https://raw.githubusercontent.com/GokGokalp/MetroBus/master/misc/metrobus-logo.png)

Lightweight messaging wrapper of _MassTransit_

[![Build status](https://ci.appveyor.com/api/projects/status/o39lu901yp69hkl0?svg=true)](https://ci.appveyor.com/project/GokGokalp/metrobus)
[![NuGet version](https://badge.fury.io/nu/MetroBus.svg)](https://badge.fury.io/nu/MetroBus)

### NuGet Packages
``` 
PM> Install-Package MetroBus 
```

####Features:
- Currently only support RabbitMQ transport
- Provide easily create **Producer** and **Consumer** for Pub/Sub
- Provide easily  **Request/Response** conversation
- Include incremental auto retry policy
- Include circuit breaker
- Include rate limiter

Usage:
-----

Initializing bus instance for **Producer**:

```cs
ISendEndpoint bus = await MetroBusInitializer.Instance.UseRabbitMq(string rabbitMqUri, string rabbitMqUserName, string rabbitMqPassword)
													.InitializeProducer(string queueName);
```


after bus instance initializing then you can use _Send_ method with your queues channel _TCommand_ type.

```cs
bus.Send<TCommand>(new
			{
				SomeProperty = SomeValue
			}
		);
```


using for **Consumer**:

```cs
static void Main(string[] args)
{
	BusHandle bus = MetroBusInitializer.Instance.UseRabbitMq(string rabbitMqUri, string rabbitMqUserName, string rabbitMqPassword)
							.InitializeConsumer<TCommandConsumer>(string queueName)
							// or .InitializeConsumer(string queueName, () => new TCommandConsumer())
							.Start();
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


**PS**: **Publisher** and **Consumer** services must be used same _TCommand_ interface. This is important for MassTransit integration. Also one other thing is _rabbitMqUri_ parameter must start with "rabbitmq://" prefix.


There are several options you can set via fluent interface:

- `.UseIncrementalRetryPolicy(int retryLimit, int initialIntervalFromMinute, int intervalIncrementFromMinute, params Exception[] retryOnSpecificExceptionType)`
- `.UseCircuitBreaker(int tripThreshold, int activeThreshold, int resetInterval)`
- `.UseRateLimiter(int rateLimit, int interval)`