using System.Diagnostics;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MetroBus.Tests
{
    public class FooCommandConsumer : IConsumer<IFooCommandContract>
    {
        public async Task Consume(ConsumeContext<IFooCommandContract> context)
        {
            var command = context.Message;

            if (command.Message == "Say hello!")
            {
                await Task.Run(() =>  Debug.WriteLine("Hello!"));
                Assert.IsTrue(true);
            }
            else
            {
                Assert.IsTrue(false);
            }
        }
    }
}