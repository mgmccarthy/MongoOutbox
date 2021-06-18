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

            //builder.ConfigureAppConfiguration((hostContext, configApp) =>
            //{
            //    configApp.AddJsonFile("secretsettings.json", true);
            //});

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

                var transport = endpointConfiguration.UseTransport<LearningTransport>();

                endpointConfiguration.SendFailedMessagesTo("MongoOutbox.Error");
                endpointConfiguration.AuditProcessedMessagesTo("MongoOutbox.Audit");

                //mongodb://mongodb0.example.com:27017,mongodb1.example.com:27017,mongodb2.example.com:27017/?replicaSet=myRepl
                //endpointConfiguration.UsePersistence<InMemoryPersistence>();
                var persistence = endpointConfiguration.UsePersistence<MongoPersistence>();
                persistence.MongoClient(new MongoClient("mongodb://localhost:27011"));
                //persistence.MongoClient(new MongoClient("mongodb://localhost:27011/?replicaSet=rs0"));
                //var connString = "mongodb://localhost:27029,localhost:27027,localhost:27028?connect=replicaSet";
                //persistence.MongoClient(new MongoClient("mongodb://localhost:27011,localhost:27012,localhost:27013/?replicaSet=rs0"));
                //persistence.MongoClient(new MongoClient("mongodb://localhost:27011,localhost:27012,localhost:27013?connect=replicaSet"));

                //var settings = new MongoClientSettings
                //{
                //    Servers = new[]
                //    {
                //        new MongoServerAddress("localhost", 27011),
                //        new MongoServerAddress("localhost", 27012),
                //        new MongoServerAddress("localhost", 27013)
                //    },
                //    ConnectionMode = ConnectionMode.Automatic,
                //    ReplicaSetName = "rs0"
                //};
                //var client = new MongoClient(settings);
                //persistence.MongoClient(client);

                persistence.DatabaseName("MongoOutboxEndpoint1");

                endpointConfiguration.UseSerialization<NewtonsoftSerializer>();
                endpointConfiguration.DefineCriticalErrorAction(OnCriticalError);

                endpointConfiguration.EnableOutbox();

                return endpointConfiguration;

            });

            builder.ConfigureServices((context, services) =>
            {
                //services.AddSingleton<IMongoClient>(provider => new MongoClient("mongodb://root:rootpassword@127.0.0.1:27017"));
                services.AddSingleton<IMongoClient>(provider => new MongoClient("mongodb://localhost:27011"));
                //services.AddSingleton<IMongoClient>(provider => new MongoClient("mongodb://localhost:27011/?replicaSet=rs0"));
                //services.AddSingleton<IMongoClient>(provider => new MongoClient("mongodb://localhost:27011,localhost:27012,localhost:27013/?replicaSet=rs0"));
                //services.AddSingleton<IMongoClient>(provider => new MongoClient("mongodb://localhost:27011,localhost:27012,localhost:27013?connect=replicaSet"));
                //var settings = new MongoClientSettings
                //{
                //    Servers = new[]
                //    {
                //        new MongoServerAddress("localhost", 27011),
                //        new MongoServerAddress("localhost", 27012),
                //        new MongoServerAddress("localhost", 27013)
                //    },
                //    ConnectionMode = ConnectionMode.Automatic,
                //    ReplicaSetName = "rs0"
                //};
                //var client = new MongoClient(settings);
                //services.AddSingleton<IMongoClient>(provider => client);

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