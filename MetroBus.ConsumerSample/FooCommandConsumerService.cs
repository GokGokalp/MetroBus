using System.Configuration;
using MassTransit;
using MetroBus.ConsumerSample.Contracts;

namespace MetroBus.ConsumerSample
{
    //You can use this class with TopShelf as a windows service.
    public class FooCommandConsumerService
    {
        private readonly IBusControl _consumerBusControl;

        public FooCommandConsumerService()
        {
            var rabbitMqUri = ConfigurationManager.AppSettings["RabbitMqUri"];
            var rabbitMqUserName = ConfigurationManager.AppSettings["RabbitMqUserName"];
            var rabbitMqPassword = ConfigurationManager.AppSettings["RabbitMqPassword"];

            _consumerBusControl = MetroBusInitializer.Instance
                                    .UseRabbitMq(rabbitMqUri, rabbitMqUserName, rabbitMqPassword)
                                    .RegisterConsumer<FooCommandConsumer>("foo.create-queue")
                                    .RegisterConsumer<FirstFooCreatedEventConsumer>("foo-created.first-action-queue")
                                    .RegisterConsumer<SecondFooCreatedEventConsumer>("foo-created.second-action-queue")
                                    .Build();
        }

        public async void Start()
        {
            await _consumerBusControl.Publish<IFooCreatedEvent>(new FooCreatedEvent
            {
                Id = 5
            });

            await _consumerBusControl.StartAsync();
        }

        public async void Stop()
        {
            await _consumerBusControl.StopAsync();
        }
    }
}