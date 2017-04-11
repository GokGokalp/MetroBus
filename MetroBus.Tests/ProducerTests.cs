using System.Configuration;
using System.Threading.Tasks;
using MassTransit;
using MetroBus.ConsumerSample.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MetroBus.Tests
{
    [TestClass]
    public class ProducerTests
    {
        private ISendEndpoint _producerEndpoint;
        private string _rabbitMqUri;
        private string _rabbitMqUserName;
        private string _rabbitMqPassword;
        private string _queueName;

        [TestInitialize]
        public void Initialize()
        {
            _rabbitMqUri = ConfigurationManager.AppSettings["RabbitMqUri"];
            _rabbitMqUserName = ConfigurationManager.AppSettings["RabbitMqUserName"];
            _rabbitMqPassword = ConfigurationManager.AppSettings["RabbitMqPassword"];
            _queueName = ConfigurationManager.AppSettings["FooQueue"];
        }

        [TestMethod]
        public async Task Send_WithFooCommand_SendQueue()
        {
            //Arrange
            _producerEndpoint = await MetroBusInitializer.Instance.UseRabbitMq(_rabbitMqUri, _rabbitMqUserName, _rabbitMqPassword)
                                    .InitializeProducer(_queueName);

            //Act
            await _producerEndpoint.Send<IFooCommandContract>(new
            {
                Message = "Say hello!"
            });
        }
    }
}