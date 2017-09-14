using System;
using System.Threading.Tasks;
using MassTransit;
using MetroBus.ConsumerSample.Contracts;

namespace MetroBus.ConsumerSample
{
    public class SecondFooCreatedEventConsumer : IConsumer<IFooCreatedEvent>
    {
        public async Task Consume(ConsumeContext<IFooCreatedEvent> context)
        {
            throw new Exception("Whoaaa...... Booom...");
            await Console.Out.WriteLineAsync($"Second consumer says: Foo:{context.Message.Id} created.");
        }
    }
}