using System;
using NServiceBus;

namespace MongoOutbox.Shared
{
    public class CreateOrder : ICommand
    {
        public Guid OrderId { get; set; }
    }
}
