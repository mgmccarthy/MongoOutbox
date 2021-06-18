using NServiceBus;

namespace MongoOutbox.Shared
{
    public class OrderCreated : IEvent
    {
        public Order Order { get; set; }
    }
}
