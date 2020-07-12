using MassTransit;
using System;

namespace MetroBus.Sample.Consumer
{
    class Program
    {
        static void Main(string[] args)
        {
            string rabbitMqUri = "rabbitmq://localhost:5672";
            string rabbitMqUserName = "";
            string rabbitMqPassword = "";
            string queueName = "foo.queue";

            var busControl = MetroBusInitializer.Instance
                .UseRabbitMq(rabbitMqUri, rabbitMqUserName, rabbitMqPassword)
                .RegisterConsumer<CreateFooCommandConsumer>(queueName)
                .RegisterConsumer<FooCreatedEventConsumer>(null)
                .Build();

            Console.WriteLine("Bus starting...");
            busControl.Start();
        }
    }
}