using System;
using System.Threading.Tasks;
using MassTransit;
using MetroBus.ConsumerSample.Contracts;

namespace MetroBus.ConsumerSample
{
    public class FooCommandConsumer : IConsumer<IFooCommandContract>
    {
        public async Task Consume(ConsumeContext<IFooCommandContract> context)
        {
            var command = context.Message;

            await Console.Out.WriteLineAsync(command.Message);

            //do somethings...
        }
    }
}