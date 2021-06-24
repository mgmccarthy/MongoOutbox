# MongoOutbox

This repo demonstrates how to use NServiceBus Outbox with MongoDb for persistence with RabbitMQ as the transport.

Although the handlers in the project contain a direct reference to `var session = context.SynchronizedStorageSession.GetClientSession();` to get a hold of the NSB Persistence managed IMongoDbClient, there are ways to use the NServiceBus pipeline to write behaviors which can "float" the context into handlers remembering to first get, then assign the session (`IClientHandleSession`) in the handler code is taken care of by NSB infrastrcutrue code.

At a high level, there are three endpoints: 
- .Client, which is run as a SendOnly endpoint. This endpoint sends a new CreateOrder command to Enpoint1 every 5 seconds.
- .Endpoint1, which handles CreateOrder. In the handler, an "Order" in inserted into the Mongo and an OrderCreated event is published
- .Endpoint2, which handler OrderCreated. In the handler, it prints out the info of the event to the console.

You'll need docker installed and running and .NET Core 3.1 on your machine in order to run the solution.

## Running the App
The master branch contains a docker-compose.yml file that stands up a replica set of MongoDb (required to run NServiceBus Outbox using MongoDb as persistence) and RabbitMq.

In order to run the code in the master branch, follow these steps

- run `docker-compose up` in the root repo directory. This will fetch the latest MongoDb images (if not already present on your computer) and start up a MongoDb replica set.
- in order to add each instance to the replica set, open a new console as administrator and execute the following commands
     - `docker exec -it mongo1 /bin/bash`
     - `mongo`
     - `rs.initiate( { _id : 'rs0', members: [ { _id : 0, host : "mongo1:27017" }, { _id : 1, host : "mongo2:27017" }, { _id : 2, host : "mongo3:27017" } ] })`
     
this will add all MongoDb instances to the `rs0`replica set.

To connect to the replica set, download Robot 3T (a mongo client) and connect to the three instances like this:
- localhost:27011 (master)
- localhost:27012 (master)
- localhost:27013 (master)

27011 is primary, 012 and 013 are the secondaries (or replicas). There is no security for any of the databases.

To access RabbitMQ administration, go to http://localhost:15672/ with username `rabbitmq` and password `rabbitmq`.

To run the solution, check that the MongoOutbox solution has three projects set to startup:
- MongoOutbox.Client
- MongoOutbox.Endpoint1
- MongoOutbox.Endpoint2

Next, hit F5. All three endpoints should stand up. .Client will send a new command, CreateOrder, every 5 seconds. Endpoint1 will handle CreateOrder, insert an Order into Mongo, then publics an event, OrderCreated. Endpoint2 will handle the OrderCreated event and simply write to consle that the event was handled.

This is what the MongoDb databases and collection in each database should look like:
![MongoDatabases](MongoDatabases.png)

- MongoOutbox: this is the "business" database. Aka, where Orders are written. It has nothing to do with NSB Outbox
- MongoOutboxEndpoint1: this is MongoOutbox.Endpoint1's outbox storage
- MongoOutboxEndpoint2: this is MongoOutbox.Endpoint2's outbox storage

By convention, NServiceBus keeps an outbox storage db per endpoint.