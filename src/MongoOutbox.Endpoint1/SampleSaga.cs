using System;
using System.Threading.Tasks;
using MongoOutbox.Shared;
using NServiceBus;
using NServiceBus.Logging;

namespace MongoOutbox.Endpoint1
{
    public class SampleSaga : Saga<SampleSaga.SampleSagaData>, 
        IAmStartedByMessages<OrderCreated>, 
        IHandleTimeouts<SampleSaga.TimeoutState>

    {
        private static readonly ILog Log = LogManager.GetLogger<SampleSaga>();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SampleSagaData> mapper)
        {
            mapper.ConfigureMapping<OrderCreated>(message => message.Order.Id).ToSaga(sagaData => sagaData.OrderId);
        }

        public Task Handle(OrderCreated message, IMessageHandlerContext context)
        {
            Log.Info($"SampleSaga started with OrderId: {message.Order.Id}");
            Log.Info($"Setting timeout for 15 seconds");
            return RequestTimeout<TimeoutState>(context, TimeSpan.FromSeconds(15));
        }

        public Task Timeout(TimeoutState state, IMessageHandlerContext context)
        {
            Log.Info($"SampleSaga timeout fired, marking saga complete");
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