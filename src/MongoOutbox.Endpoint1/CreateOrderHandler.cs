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

            await UseInjectedIMongoClient(message, context);
            //await UseOutboxManagedIMongoClient(message, context);
            //await UsePipelineManagedIMongoClient(message, context);
            
            await context.Publish(new OrderCreated
            {
                Order = message.Order
            });
        }

       
        private async Task UseInjectedIMongoClient(CreateOrder message, IMessageHandlerContext context)
        {
            //https://docs.particular.net/persistence/mongodb/?#transactions-shared-transactions
            var session = context.SynchronizedStorageSession.GetClientSession();

            var database = mongoClient.GetDatabase("MongoOutbox");
            var collection = database.GetCollection<Order>("orders");
            //note the Outbox managed session passed into InsertOneAsync
            //this allows both db and transport ops to participate in an ACID transaction for exactly once message guarentee
            await collection.InsertOneAsync(session, message.Order);
        }

        private static async Task UseOutboxManagedIMongoClient(CreateOrder message, IMessageHandlerContext context)
        {
            //https://docs.particular.net/persistence/mongodb/?#transactions-shared-transactions
            var session = context.SynchronizedStorageSession.GetClientSession();

            //note how IMongoClient is accessed from IClientSessionHandle (session) to get an instance of IMongoDatabase
            //this IMongoClient represents the Outbox managed IMongoClient instance, so no need to pass a different IClientSessionHandle into .InsertOneAsync like with UseInjectedIMongoClient
            var database = session.Client.GetDatabase("MongoOutbox");
            var collection = database.GetCollection<Order>("orders");
            await collection.InsertOneAsync(message.Order);
        }

        private static async Task UsePipelineManagedIMongoClient(CreateOrder message, IMessageHandlerContext context)
        {
            //.GetDatabase is an extension method which obtains an instance of IMongoDatabase created and managed by SynchronizedStorageSessionBehavior, which is registered at startup in the NSB pipeline
            var database = context.GetDatabase();
            var collection = database.GetCollection<Order>("orders");
            await collection.InsertOneAsync(message.Order);
        }

    }
}
