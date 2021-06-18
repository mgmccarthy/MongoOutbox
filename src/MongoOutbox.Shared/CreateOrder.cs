using NServiceBus;

namespace MongoOutbox.Shared
{
    public class CreateOrder : ICommand
    {
        public Order Order { get; set; }
    }
}
