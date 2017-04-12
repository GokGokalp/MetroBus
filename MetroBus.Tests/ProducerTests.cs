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
        public async Task Send_WithCreateFooCommand_SendQueue()
        {
            //Arrange
            ISendEndpoint producerEndpoint =
                await
                    MetroBusInitializer.Instance.UseRabbitMq(_rabbitMqUri, _rabbitMqUserName, _rabbitMqPassword)
                        .InitializeProducer(_queueName);

            //Act
            await producerEndpoint.Send<ICreateFooCommand>(new
            {
                Message = "Create me!"
            });
        }

        [TestMethod]
        public async Task Publish_WithFooCreatedEvent_PublishEventToExchange()
        {
            //Arrange
            IBusControl busControl = MetroBusInitializer.Instance.UseRabbitMq(_rabbitMqUri, _rabbitMqUserName, _rabbitMqPassword)
                                     .Build();

            //Act
            await busControl.Publish<IFooCreatedEvent>(new
            {
                Id = 1
            });
        }

        [TestMethod]
        public async Task RequestResponse_WithCreateFooRequest_CreateFooAndReturnCreateFooResponse()
        {
            //Arrange
            IRequestClient<ICreateFooRequest, ICreateFooResponse> client = MetroBusInitializer.Instance
                                                    .UseRabbitMq(_rabbitMqUri, _rabbitMqUserName, _rabbitMqPassword)
                                                    .InitializeRequestClient<ICreateFooRequest, ICreateFooResponse>(_queueName);

            //Act
            ICreateFooResponse response = await client.Request(new CreateFooRequest
            {
                Message = "Create me now!"
            });
        }
    }
}