using System.Configuration;
using MassTransit;

namespace MetroBus.ConsumerSample
{
    //You can use this class with TopShelf as a windows service.
    public class FooCommandConsumerService
    {
        private IBusControl _consumerBusControl;
        private string _rabbitMqUri;
        private string _rabbitMqUserName;
        private string _rabbitMqPassword;
        private string _queueName;

        public FooCommandConsumerService()
        {
            _rabbitMqUri = ConfigurationManager.AppSettings["RabbitMqUri"];
            _rabbitMqUserName = ConfigurationManager.AppSettings["RabbitMqUserName"];
            _rabbitMqPassword = ConfigurationManager.AppSettings["RabbitMqPassword"];
            _queueName = ConfigurationManager.AppSettings["FooQueue"];

            _consumerBusControl = MetroBusInitializer.Instance.UseRabbitMq(_rabbitMqUri, _rabbitMqUserName, _rabbitMqPassword)
                             .InitializeConsumer<FooCommandConsumer>(_queueName).Build();
        }

        public void Start()
        {
            _consumerBusControl.Start();
        }

        public void Stop()
        {
            _consumerBusControl.Stop();
        }
    }
}