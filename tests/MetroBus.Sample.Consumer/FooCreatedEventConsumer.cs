using MassTransit;
using MetroBus.Sample.Contracts;
using System;
using System.Threading.Tasks;

namespace MetroBus.Sample.Consumer
{
    public class FooCreatedEventConsumer : IConsumer<IFooCreatedEvent>
    {
        public async Task Consume(ConsumeContext<IFooCreatedEvent> context)
        {
            var @event = context.Message;

            await Console.Out.WriteLineAsync($"FooCreatedEvent {@event.Id.ToString()}");

            //do somethings...
        }
    }
}