using System;
using System.Threading.Tasks;
using MongoOutbox.Shared;
using NServiceBus;

namespace MongoOutbox.Client
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            Console.Title = "MongoDb.Client";

            var endpointConfiguration = new EndpointConfiguration("MongoDb.Client");

            var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
            transport.UseConventionalRoutingTopology();
            transport.ConnectionString("host=localhost;username=rabbitmq;password=rabbitmq");
            transport.Routing().RouteToEndpoint(typeof(CreateOrder), "MongoOutbox.Endpoint1");

            endpointConfiguration.EnableInstallers();
            endpointConfiguration.SendFailedMessagesTo("MongoOutbox.Error");
            endpointConfiguration.AuditProcessedMessagesTo("MongoOutbox.Audit");
            endpointConfiguration.EnableInstallers();
            endpointConfiguration.UseSerialization<NewtonsoftSerializer>();
            endpointConfiguration.SendOnly();

            var endpointInstance = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);

            await CreateOrder(endpointInstance);
            //while (true)
            //{
            //    await CreateOrder(endpointInstance);
            //    await Task.Delay(5000);
            //}
        }

        private static async Task CreateOrder(IEndpointInstance endpointInstance)
        {
            var sendMessage = new CreateOrder
            {
                Order = new Order
                {
                    Id = Guid.NewGuid()
                }
            };

            Console.WriteLine($"Sending CreateOrder with OrderId: {sendMessage.Order.Id}");

            var sendOptions = new SendOptions();
            sendOptions.RequireImmediateDispatch();
            await endpointInstance.Send(sendMessage, sendOptions);
        }
    }
}