using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoOutbox.Shared;
using NServiceBus;
using NServiceBus.Logging;

namespace MongoOutbox.Endpoint1
{
    public class CreateOrderHandler : IHandleMessages<CreateOrder>
    {
        private readonly IMongoClient mongoClient;
        private static readonly ILog Log = LogManager.GetLogger<CreateOrderHandler>();

        public CreateOrderHandler(IMongoClient mongoClient)
        {
            this.mongoClient = mongoClient;
        }

        public async Task Handle(CreateOrder message, IMessageHandlerContext context)
        {
            Log.Info($"Handling CreateOrder with Id: {message.Order.Id}");

            var database = mongoClient.GetDatabase("MongoOutbox");
            var collection = database.GetCollection<Order>("orders");

            //https://docs.particular.net/persistence/mongodb/?#transactions-shared-transactions
            //var session = context.SynchronizedStorageSession.GetClientSession();
            //await collection.InsertOneAsync(session, message.Message);

            await collection.InsertOneAsync(message.Order);

            await context.Publish(new OrderCreated
            {
                Order = message.Order
            });
        }
    }
}
