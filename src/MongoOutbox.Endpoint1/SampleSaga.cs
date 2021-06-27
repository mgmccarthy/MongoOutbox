using System;
using System.Threading.Tasks;
using MongoOutbox.Shared;
using NServiceBus;
using NServiceBus.Logging;

namespace MongoOutbox.Endpoint1
{
    //WARNING: this is a bad example of saga usage. A saga that is started by one message, doesn't handle any other messages and only fires a timeout can easily be implemented using
    //an NSB handler with delayed delivery. It's only here to show what the saga storage artifacts looks like in MongoDb
    public class SampleSaga : Saga<SampleSaga.SampleSagaData>, 
        IAmStartedByMessages<OrderCreated>, 
        IHandleTimeouts<SampleSaga.TimeoutState>

    {
        private static readonly ILog Log = LogManager.GetLogger<SampleSaga>();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SampleSagaData> mapper)
        {
            mapper.ConfigureMapping<OrderCreated>(message => message.OrderId).ToSaga(sagaData => sagaData.OrderId);
        }

        public Task Handle(OrderCreated message, IMessageHandlerContext context)
        {
            Log.Info($"SampleSaga started with OrderId: {message.OrderId}");
            Log.Info($"Setting timeout for 15 seconds");
            return RequestTimeout<TimeoutState>(context, TimeSpan.FromSeconds(15));
        }

        public Task Timeout(TimeoutState state, IMessageHandlerContext context)
        {
            Log.Info($"SampleSaga timeout fired for OrderId: {Data.OrderId}, marking saga complete");
            MarkAsComplete();
            return Task.CompletedTask;
        }

        public class SampleSagaData : ContainSagaData
        {
            public Guid OrderId { get; set; }
        }

        public class TimeoutState { }
    }
}