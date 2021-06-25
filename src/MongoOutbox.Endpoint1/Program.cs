using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using NServiceBus;

namespace MongoOutbox.Endpoint1
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            Console.Title = "MongoOutbox.Endpoint1";
            await CreateHostBuilder(args).Build().RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);
            builder.UseConsoleLifetime();

            builder.UseMicrosoftLogFactoryLogging();
            builder.ConfigureLogging((ctx, logging) =>
            {
                logging.AddConfiguration(ctx.Configuration.GetSection("Logging"));
                logging.AddConsole();
            });

            builder.UseNServiceBus(ctx =>
            {
                var endpointConfiguration = new EndpointConfiguration("MongoOutbox.Endpoint1");
                endpointConfiguration.EnableInstallers();

                var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
                transport.UseConventionalRoutingTopology();
                transport.ConnectionString("host=localhost;username=rabbitmq;password=rabbitmq");

                var persistence = endpointConfiguration.UsePersistence<MongoPersistence>();
                persistence.MongoClient(new MongoClient("mongodb://localhost:27011"));
                persistence.DatabaseName("MongoOutboxEndpoint1");

                endpointConfiguration.SendFailedMessagesTo("MongoOutbox.Error");
                endpointConfiguration.AuditProcessedMessagesTo("MongoOutbox.Audit");

                endpointConfiguration.UseSerialization<NewtonsoftSerializer>();
                endpointConfiguration.DefineCriticalErrorAction(OnCriticalError);

                endpointConfiguration.EnableOutbox();

                var pipeline = endpointConfiguration.Pipeline;
                pipeline.Register(new SynchronizedStorageSessionBehavior(), "SynchronizedStorageSessionBehavior");

                return endpointConfiguration;
            });

            builder.ConfigureServices((context, services) =>
            {
                services.AddSingleton<IMongoClient>(provider => new MongoClient("mongodb://localhost:27011"));
                //https://kevsoft.net/2020/06/25/storing-guids-as-strings-in-mongodb-with-csharp.html
                var pack = new ConventionPack { new GuidAsStringRepresentationConvention() };
                ConventionRegistry.Register("GUIDs as strings Conventions", pack, type => type.Namespace.StartsWith("MongoOutbox"));
            });

            return builder;
        }

        private static async Task OnCriticalError(ICriticalErrorContext context)
        {
            var fatalMessage = $"The following critical error was " +
                               $"encountered: {Environment.NewLine}{context.Error}{Environment.NewLine}Process is shutting down. " +
                               $"StackTrace: {Environment.NewLine}{context.Exception.StackTrace}";

            EventLog.WriteEntry(".NET Runtime", fatalMessage, EventLogEntryType.Error);

            try
            {
                await context.Stop().ConfigureAwait(false);
            }
            finally
            {
                Environment.FailFast(fatalMessage, context.Exception);
            }
        }
    }
}