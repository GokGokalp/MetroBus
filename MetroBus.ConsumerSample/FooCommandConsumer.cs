using System;
using System.Threading.Tasks;
using MassTransit;
using MetroBus.ConsumerSample.Contracts;

namespace MetroBus.ConsumerSample
{
    public class FooCommandConsumer : IConsumer<ICreateFooCommand>
    {
        public async Task Consume(ConsumeContext<ICreateFooCommand> context)
        {
            var command = context.Message;

            await Console.Out.WriteLineAsync(command.Message);

            //do somethings...
        }
    }
}