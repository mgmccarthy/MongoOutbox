using System.Threading.Tasks;
using MongoOutbox.Shared;
using NServiceBus;

namespace MongoOutbox.Endpoint2
{
    public class OrderCreatedHandler : IHandleMessages<OrderCreated>
    {
        public Task Handle(OrderCreated message, IMessageHandlerContext context)
        {
            return Task.CompletedTask;
        }
    }
}
