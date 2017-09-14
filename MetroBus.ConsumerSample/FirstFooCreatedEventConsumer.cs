using System;
using System.Threading.Tasks;
using MassTransit;
using MetroBus.ConsumerSample.Contracts;

namespace MetroBus.ConsumerSample
{
    public class FirstFooCreatedEventConsumer : IConsumer<IFooCreatedEvent>
    {
        public async Task Consume(ConsumeContext<IFooCreatedEvent> context)
        {
            await Console.Out.WriteLineAsync($"First consumer says: Foo:{context.Message.Id} created.");
        }
    }
}