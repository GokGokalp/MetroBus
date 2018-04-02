using MassTransit;
using MetroBus.Sample.Contracts;
using System;
using System.Threading.Tasks;

namespace MetroBus.Sample.Consumer
{
    public class CreateFooCommandConsumer : IConsumer<ICreateFooCommand>
    {
        public async Task Consume(ConsumeContext<ICreateFooCommand> context)
        {
            var command = context.Message;

            await Console.Out.WriteLineAsync($"CreateFooCommand {command.Id.ToString()}");

            //do somethings...
        }
    }
}