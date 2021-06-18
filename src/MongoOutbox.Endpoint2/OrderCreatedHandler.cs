using System.Threading.Tasks;
using MongoOutbox.Shared;
using NServiceBus;
using NServiceBus.Logging;

namespace MongoOutbox.Endpoint2
{
    public class OrderCreatedHandler : IHandleMessages<OrderCreated>
    {
        private static readonly ILog Log = LogManager.GetLogger<OrderCreatedHandler>();

        public Task Handle(OrderCreated message, IMessageHandlerContext context)
        {
            Log.Info($"Handling OrderCreated with Id: {message.Order.Id}");
            return Task.CompletedTask;
        }
    }
}
