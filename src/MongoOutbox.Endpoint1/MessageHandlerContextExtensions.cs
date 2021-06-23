using MongoDB.Driver;
using NServiceBus;

namespace MongoOutbox.Endpoint1
{
    public static class MessageHandlerContextExtensions
    {
        public static IMongoDatabase Database(this IMessageHandlerContext context)
        {
            return context.Extensions.Get<IMongoDatabase>();
        }
    }
}
