using MassTransit;
using MassTransit.Util;
using MetroBus.Sample.Contracts;

namespace MetroBus.Sample.Producer
{
    class Program
    {
        static void Main(string[] args)
        {
            string rabbitMqUri = "rabbitmq://localhost:5672";
            string rabbitMqUserName = "";
            string rabbitMqPassword = "";

            SendEvent(rabbitMqUri, rabbitMqUserName, rabbitMqPassword);
            SendCommand(rabbitMqUri, rabbitMqUserName, rabbitMqPassword);
        }

        public static void SendEvent(string rabbitMqUri, string rabbitMqUserName, string rabbitMqPassword)
        {
            IBusControl busControl = MetroBusInitializer.Instance.UseRabbitMq(rabbitMqUri, rabbitMqUserName, rabbitMqPassword)
                         .InitializeEventProducer();

            TaskUtil.Await(busControl.Publish<IFooCreatedEvent>(new
            {
                Id = 1
            }));
        }

        public static void SendCommand(string rabbitMqUri, string rabbitMqUserName, string rabbitMqPassword)
        {
            string queueName = "foo.queue";

            ISendEndpoint busControl = MetroBusInitializer.Instance.UseRabbitMq(rabbitMqUri, rabbitMqUserName, rabbitMqPassword)
                         .InitializeCommandProducer(queueName);

            TaskUtil.Await(busControl.Send<ICreateFooCommand>(new
            {
                Id = 1
            }));
        }
    }
}