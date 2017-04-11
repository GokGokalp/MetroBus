using System.Threading;

namespace MetroBus.ConsumerSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var fooCommandConsumerService = new FooCommandConsumerService();

            fooCommandConsumerService.Start();

            Thread.Sleep(10000);

            fooCommandConsumerService.Stop();
        }
    }
}