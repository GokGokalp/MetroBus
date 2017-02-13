using System.Configuration;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MetroBus.Tests
{
    [TestClass]
    public class PubSubTests
    {
        private ISendEndpoint _producerBus;
        private BusHandle _busHandle;
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
            _producerBus = await MetroBusInitializer.Instance.UseRabbitMq(_rabbitMqUri, _rabbitMqUserName, _rabbitMqPassword)
                                     .InitializeProducer(_queueName);

            //Act
            await _producerBus.Send<IFooCommandContract>(new
            {
                Message = "Say hello!"
            });
        }

        [TestMethod]
        public async Task InitializeConsumer_WithFooCommandConsumer_ConsumeFooQueueAndWriteHelloToDebugWindow()
        {
            //Arrange, Act
            _busHandle = await MetroBusInitializer.Instance.UseRabbitMq(_rabbitMqUri, _rabbitMqUserName, _rabbitMqPassword)
                                    .InitializeConsumer<FooCommandConsumer>(_queueName)
                                    .Start();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _producerBus = null;

            _busHandle?.Stop();

            _busHandle = null;
        }
    }
}