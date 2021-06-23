using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Pipeline;

namespace MongoOutbox.Endpoint1
{
    public class SynchronizedStorageSessionBehavior : Behavior<IInvokeHandlerContext>
    {
        public override async Task Invoke(IInvokeHandlerContext context, Func<Task> next)
        {
            var session = context.SynchronizedStorageSession.GetClientSession();
            var database = session.Client.GetDatabase("MongoOutbox");
            context.Extensions.Set(database);
            await next();
        }
    }
}
