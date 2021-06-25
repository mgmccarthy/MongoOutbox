using System;
using NServiceBus;

namespace MongoOutbox.Shared
{
    public class OrderCreated : IEvent
    {
        public Guid OrderId { get; set; }
    }
}
