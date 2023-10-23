<div align="center">  
  
  ![Banner- Memphis dev streaming  (1)](https://github.com/memphisdev/memphis.net/assets/107035359/3ac8b249-bc52-4e02-ad79-7001810f9b04)

</div>

<div align="center">

  <h4>

**[Memphis](https://memphis.dev)** is an intelligent, frictionless message broker.<br>Made to enable developers to build real-time and streaming apps fast.

  </h4>
  
  <a href="https://landscape.cncf.io/?selected=memphis"><img width="200" alt="CNCF Silver Member" src="https://github.com/cncf/artwork/raw/master/other/cncf-member/silver/white/cncf-member-silver-white.svg#gh-dark-mode-only"></a>
  
</div>

<div align="center">
  
  <img width="200" alt="CNCF Silver Member" src="https://github.com/cncf/artwork/raw/master/other/cncf-member/silver/color/cncf-member-silver-color.svg#gh-light-mode-only">
  
</div>
 
 <p align="center">
  <a href="https://memphis.dev/pricing/">Cloud</a> - <a href="https://memphis.dev/docs/">Docs</a> - <a href="https://twitter.com/Memphis_Dev">Twitter</a> - <a href="https://www.youtube.com/channel/UCVdMDLCSxXOqtgrBaRUHKKg">YouTube</a>
</p>

<p align="center">
<a href="https://discord.gg/WZpysvAeTf"><img src="https://img.shields.io/discord/963333392844328961?color=6557ff&label=discord" alt="Discord"></a>
<a href="https://github.com/memphisdev/memphis/issues?q=is%3Aissue+is%3Aclosed"><img src="https://img.shields.io/github/issues-closed/memphisdev/memphis?color=6557ff"></a> 
  <img src="https://img.shields.io/npm/dw/memphis-dev?color=ffc633&label=installations">
<a href="https://github.com/memphisdev/memphis/blob/master/CODE_OF_CONDUCT.md"><img src="https://img.shields.io/badge/Code%20of%20Conduct-v1.0-ff69b4.svg?color=ffc633" alt="Code Of Conduct"></a> 
<a href="https://docs.memphis.dev/memphis/release-notes/releases/v0.4.2-beta"><img alt="GitHub release (latest by date)" src="https://img.shields.io/github/v/release/memphisdev/memphis?color=61dfc6"></a>
<img src="https://img.shields.io/github/last-commit/memphisdev/memphis?color=61dfc6&label=last%20commit">
</p>

Memphis.dev is more than a broker. It's a new streaming stack.<br><br>
It accelerates the development of real-time applications that require<br>
high throughput, low latency, small footprint, and multiple protocols,<br>with minimum platform operations, and all the observability you can think of.<br><br>
Highly resilient, distributed architecture, cloud-native, and run on any Kubernetes,<br>on any cloud without zookeeper, bookeeper, or JVM.

## Installation

```sh
 dotnet add package Memphis.Client -v ${MEMPHIS_CLIENT_VERSION}
```

## Update

```sh
Update-Package Memphis.Client
```

## Importing

```c#
using Memphis.Client;
```

### Connecting to Memphis

First, we need to create or use default `ClientOptions` and then connect to Memphis by using `MemphisClientFactory.CreateClient(ClientOptions opts)`.

```c#
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "<broker-address>";
    options.Username = "<username>";
    options.ConnectionToken = "<broker-token>"; // you will get it on broker creation
    options.AccountId = <account-id>; // You can find it on the profile page in the Memphis UI. This field should be set only on the cloud version of Memphis, otherwise it will be ignored
    var memphisClient = await MemphisClientFactory.CreateClient(options);
    ...
}
catch (Exception ex)
{
    Console.Error.WriteLine("Exception: " + ex.Message);
    Console.Error.WriteLine(ex);
}
```

A password-based connection would look like this (using the defualt root memphis login with Memphis open-source):

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";  
    var memphisClient = await MemphisClientFactory.CreateClient(options);
}
catch (Exception ex) {
    // handle exception
}
```

A JWT, token-based connection would look like this:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.ConnectionToken = "Token";  
    var memphisClient = await MemphisClientFactory.CreateClient(options);
}
catch (Exception ex) {
    // handle exception
}
```

Memphis needs to be configured to use token based connection. See the [docs](https://docs.memphis.dev/memphis/memphis-broker/concepts/security) for help doing this.

A TLS based connection would look like this:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Tls = new TlsOptions("tlsFileName");
    var memphisClient = await MemphisClientFactory.CreateClient(options);
}
catch (Exception ex)
{
    // handle exception
}
```

Memphis needs to configured for these use cases. To configure memphis to use TLS see the [docs](https://docs.memphis.dev/memphis/open-source-installation/kubernetes/production-best-practices#memphis-metadata-tls-connection-configuration). 


Once client created, the entire functionalities offered by Memphis are available.

### Disconnecting from Memphis

To disconnect from Memphis, call `Dispose()` on the `MemphisClient`.

```c#
await memphisClient.Dispose()
```
### Creating a Station

```c#
try
{
    // First: creating Memphis client
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "<memphis-host>";
    options.Username = "<username>";
    options.Password = "<password>";
    options.AccountId = <account-id>; // You can find it on the profile page in the Memphis UI. This field should be set only on the cloud version of Memphis, otherwise it will be ignored
    var client = await MemphisClientFactory.CreateClient(options);
    
    // Second: creaing Memphis station
    var station = await client.CreateStation(
        stationOptions: new StationOptions()
        {
            Name = "<station-name>",
            RetentionType = RetentionTypes.MAX_MESSAGE_AGE_SECONDS,
            RetentionValue = 604_800,
            StorageType = StorageTypes.DISK,
            Replicas = 1,
            IdempotencyWindowMs = 0,
            SendPoisonMessageToDls = true,
            SendSchemaFailedMessageToDls = true,
            PartitionsNumber = 3, // defaults to 1
            DlsStation = "<dls-station>" // If DlsStation is set, then DLS events will be sent to selected station as well. The default value is "" (no DLS station).
        });
}
catch (Exception ex)
{
    Console.Error.WriteLine("Exception: " + ex.Message);
    Console.Error.WriteLine(ex);
}
```

The CreateStation method is used to create a station. Using the different options available, one can programically create many different types of stations. The Memphis UI can also be used to create stations to the same effect. 

A minimal example, using all default values would simply create a station with the given name:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var station = await memphisClient.CreateStation(
        stationOptions: new StationOptions
        {
            Name = "MyNewStation"
        }
    );
}
catch (Exception ex)
{
    // handle exception
}
```

To change what criteria the station uses to decide if a message should be retained in the station, change the retention type. The different types of retention are documented [here](https://github.com/memphisdev/memphis.net#retention-types) in the dotnet README. 

The unit of the rentention value will vary depending on the RetentionTypes. The [previous link](https://github.com/memphisdev/memphis.net#retention-types) also describes what units will be used. 

Here is an example of a station which will only hold up to 10 messages:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var station = await memphisClient.CreateStation(
        stationOptions: new StationOptions
        {
            Name = "MyNewStation",
            RetentionType = RetentionTypes.MESSAGES,
            RetentionValue = 10
        }
    );  
}
catch (Exception ex)
{
    // handle exception
}
```

Memphis stations can either store Messages on disk or in memory. A comparison of those types of storage can be found [here](https://docs.memphis.dev/memphis/memphis-broker/concepts/storage-and-redundancy#tier-1-local-storage).

Here is an example of how to create a station that uses Memory as its storage type:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var station = await memphisClient.CreateStation(
        stationOptions: new StationOptions
        {
            Name = "MyNewStation",
            StorageType = StorageTypes.MEMORY
        }
    );
}
catch (Exception ex) {
    // handle exception
}
```

In order to make a station more redundant, replicas can be used. Read more about replicas [here](https://docs.memphis.dev/memphis/memphis-broker/concepts/storage-and-redundancy#replicas-mirroring). Note that replicas are only available in cluster mode. Cluster mode can be enabled in the [Helm settings](https://docs.memphis.dev/memphis/open-source-installation/kubernetes/1-installation#appendix-b-helm-deployment-options) when deploying Memphis with Kubernetes.

Here is an example of creating a station with 3 replicas:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var station = await memphisClient.CreateStation(
        stationOptions: new StationOptions
        {
            Name = "MyNewStation",
            Replicas = 3
        }
    );
}
catch (Exception ex)
{
    // handle exception
}
```

Idempotency defines how Memphis will prevent duplicate messages from being stored or consumed. The duration of time the message ID's will be stored in the station can be set with IdempotencyWindowMs. If the environment Memphis is deployed in has unreliably connection and/or a lot of latency, increasing this value might be desiriable. The default duration of time is set to two minutes. Read more about idempotency [here](https://docs.memphis.dev/memphis/memphis-broker/concepts/idempotency).

Here is an example of changing the idempotency window to 3 seconds:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var station = await memphisClient.CreateStation(
        stationOptions: new StationOptions
        {
            Name = "MyNewStation",
            IdempotenceWindowMs = 180_000
        }
    );
}
catch (Exception ex)
{
    // handle exception
}
```

The schema name is used to set a schema to be enforced by the station. The default value of "" ensures that no schema is enforced. Here is an example of changing the schema to a defined schema in schemaverse called "SensorLogs":

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var station = await memphisClient.CreateStation(
        stationOptions: new StationOptions
        {
            Name = "MyNewStation",
            SchemaName = "SensorLogs"
        }
    );
}
catch (Exception ex)
{
    // handle exception
}
```

There are two options for sending messages to the [dead-letter station(DLS)](https://docs.memphis.dev/memphis/memphis-broker/concepts/dead-letter#terminology). These are SendPoisonMessageToDls and SendSchemaFailedMessageToDls. 

Here is an example of sending poison messages to the DLS but not messages which fail to conform to the given schema.

```csharp
    try
    {
        var options = MemphisClientFactory.GetDefaultOptions();
        options.Host = "localhost";
        options.Username = "root";
        options.Password = "memphis";
        var memphisClient = await MemphisClientFactory.CreateClient(options);

        var station = await memphisClient.CreateStation(
            stationOptions: new StationOptions
            {
                Name = "MyNewStation",
                SendPoisonMessageToDls = true,
                SendSchemaFailedMessageToDls = false
            }
        );
    }
    catch (Exception ex)
    {
        // handle exception
    }
```

When either of the DLS flags are set to True, a station can also be set to handle these events. To set a station as the station to where schema failed or poison messages will be set to, use the DlsStation StationOptions:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var station = await memphisClient.CreateStation(
        stationOptions: new StationOptions
        {
            Name = "MyNewStation",
            SendPoisonMessageToDls = true,
            SendSchemaFailedMessageToDls = false,
            // DlsStation = "DeadLetterMessageStation" // Coming soon
        }
    );
}
catch (Exception ex)
{
    // handle exception
}
```

When the retention value is met, Mempihs by default will delete old messages. If tiered storage is setup, Memphis can instead move messages to tier 2 storage. Read more about tiered storage [here](https://docs.memphis.dev/memphis/memphis-broker/concepts/storage-and-redundancy#storage-tiering). Enable this setting with the option provided:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var station = await memphisClient.CreateStation(
        stationOptions: new StationOptions
        {
            Name = "MyNewStation",
            TieredStorageEnabled = true
        }
    );
}
catch (Exception ex)
{
    // handle exception
}
```

[Partitioning](https://docs.memphis.dev/memphis/memphis-broker/concepts/station#partitions) might be useful for a station. To have a station partitioned, simply change the partitions number:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var station = await memphisClient.CreateStation(
        stationOptions: new StationOptions
        {
            Name = "MyNewStation",
            PartitionsNumber = 3
        }
    );
}
catch (Exception ex)
{
    // handle exception
}
```


### Retention types

Memphis currently supports the following types of retention:

```c#
RetentionTypes.MAX_MESSAGE_AGE_SECONDS
```
The above means that every message persists for the value set in the retention value field (in seconds).

```c#
RetentionTypes.MESSAGES
```
The above means that after the maximum number of saved messages (set in retention value) has been reached, the oldest messages will be deleted.

```c#
RetentionTypes.BYTES
```
The above means that after maximum number of saved bytes (set in retention value) has been reached, the oldest messages will be deleted.

```c#
RetentionTypes.ACK_BASED
```
The above means that after a message is getting acked by all interested consumer groups it will be deleted from the Station. This retention type is for cloud users only.


### Retention Values

The `retention values` are directly related to the `retention types` mentioned above,<br> where the values vary according to the type of retention chosen.

All retention values are of type `int` but with different representations as follows:

`RetentionTypes.MAX_MESSAGE_AGE_SECONDS` is represented **in seconds**, `RetentionTypes.MESSAGES` in a **number of messages**, `RetentionTypes.BYTES` in a **number of bytes**, and finally `RetentionTypes.ACK_BASED` is not using the retention value. 

After these limits are reached oldest messages will be deleted.


### Storage Types
Memphis currently supports the following types of messages storage:

```c#
StorageTypes.DISK
```
The above means that messages persist on disk.

```c#
StorageTypes.MEMORY
```
The above means that messages persist in the main memory.

### Destroying a Station

Destroying a station will remove all its resources (including producers and consumers).
```c#
station.DestroyAsync()
```

### Creating a new Schema

```c#
await client.CreateSchema("<schema-name>", "<schema-type>", "<schema-file-path>")
```

### Enforcing a Schema on an Existing Station

```c#
await client.EnforceSchema(stationName: "<station-name>", schemaName: "<schema-name>");
```

### Deprecated - Attaching Schema

The `AttachSchema` method is depricated, use `EnforceSchema` instead.
```c#
await client.AttachSchema(stationName: "<station-name>", schemaName: "<schema-name>");
```

### Detaching a Schema from Station
```c#
await client.DetachSchema(stationName: station.Name);
```


### Produce and Consume messages

The most common client operations are `produce` to send messages and `consume` to
receive messages.

Messages are published to a station and consumed from it by creating a consumer.
Consumers are pull-based and consume all the messages in a station unless you are using a consumers group, in this case, messages are spread across all members in this group.

Memphis messages are payload agnostic. Payloads are `byte[]`.

In order to stop getting messages, you have to call `consumer.Dispose()`. Destroy will terminate regardless
of whether there are messages in flight for the client.

### Creating a Producer

```c#
try
{
   // First: creating Memphis client
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "<memphis-host>";
    options.Username = "<username>";
    options.Password = "<password>";
    var client = await MemphisClientFactory.CreateClient(options);

    // Second: creating the Memphis producer 
    var producer = await client.CreateProducer(new MemphisProducerOptions
    {
        StationName = "<memphis-station-name>",
        ProducerName = "<memphis-producer-name>"
    });
}
catch (Exception ex)
{
    Console.Error.WriteLine("Exception: " + ex.Message);
    Console.Error.WriteLine(ex);
}
```

### Producing a message

```c#
var commonHeaders = new NameValueCollection();
commonHeaders.Add("key-1", "value-1");

await producer.ProduceAsync(
    message: Encoding.UTF8.GetBytes(text),
    headers:commonHeaders
);
```


Note:
When producing to a station with more than one partition, the producer will produce messages in a Round Robin fashion between the different partitions.

The ProduceAsync method allows for the user to produce a message without discretely creating a producer. Because this creates a producer for every message, it is better to create a producer if many message need to be produced. 

For message data formats see [here](https://docs.memphis.dev/memphis/memphis-schemaverse/formats/produce-consume). 

Messages produced by ProduceAsync run asyncronously by default. By using the AsyncProduce Option this can be set to produce messages syncronously, waiting for an ack after each message is produced. By default, messages are sent while still waiting for the ack of previously sent messages. This reduces preceived network latency and will allow for producers to produce more messages however may incur a loss in reliability. 

Here is an example of a ProduceAsync method call that waits up to one minute for an acknowledgement from memphis and produces messages syncronously:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    await memphisClient.ProduceAsync(
        options: new MemphisProducerOptions
        {
            MaxAckTimeMs = 60_000,
            StationName = "MyStation",
            ProducerName = "MyProducer"
        },
        message: Encoding.UTF8.GetBytes("MyMessage"),
        asyncProduceAck: false
    );
}
catch (Exception ex)
{
    // handle exception
}
```

As discussed before in the station section, idempotency is an important feature of memphis. To achieve idempotency, an id must be assigned to messages that are being produced. Use the messageId parameter for this purpose.

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    await memphisClient.ProduceAsync(
        options: new MemphisProducerOptions
        {
            MaxAckTimeMs = 60_000,
            StationName = "MyStation",
            ProducerName = "MyProducer"
        },    
        message: Encoding.UTF8.GetBytes("MyMessage"),
        messageId: "UniqueMessageID"
    );
}
catch (Exception ex)
{
    // handle exception
}
```

To add message headers to the message, use the headers parameter. Headers can help with observability when using certain 3rd party to help monitor the behavior of memphis. See [here](https://docs.memphis.dev/memphis/memphis-broker/comparisons/aws-sqs-vs-memphis#observability) for more details.

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var headers = new NameValueCollection
    {
        { "trace_header", "track_me_123" }
    };

    await memphisClient.ProduceAsync(
    options: new MemphisProducerOptions
    {
        MaxAckTimeMs = 60_000,
        StationName = "MyStation",
        ProducerName = "MyProducer"
    },
    message: Encoding.UTF8.GetBytes("MyMessage"),
    headers: headers
    );
}
catch (Exception ex)
{
    // handle exception
}
```

Lastly, memphis can produce to a specific partition in a station. To do so, use the partitionKey parameter:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    await memphisClient.ProduceAsync(
        options: new MemphisProducerOptions
        {
            MaxAckTimeMs = 60_000,
            StationName = "MyStation",
            ProducerName = "MyProducer"
        },    
        message: Encoding.UTF8.GetBytes("MyMessage")
        partitionKey: "Partition3"
    );
}
catch (Exception ex)
{
    // handle exception
}
```

### Produce using partition key
The partition key will be used to produce messages to a spacific partition.

```csharp
await producer.ProduceAsync(
    message: Encoding.UTF8.GetBytes(text),
    headers:commonHeaders, 
    partitionKey:"<partition-key>"
);
```

### Produce using partition number
The partition number will be used to produce messages to a spacific partition.

```csharp
await producer.ProduceAsync(
    message: Encoding.UTF8.GetBytes(text),
    headers:commonHeaders, 
    partitionNumber:<int> // default is -1
);
```

### Destroying a Producer

```c#
await producer.DestroyAsync()
```

### Creating a Consumer

```c#
try
{
    // First: creating Memphis client
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "<memphis-host>";
    options.Username = "<username>";
    options.Password = "<password>";
    var client = await MemphisClientFactory.CreateClient(options);
    
    // Second: creaing Memphis consumer
    var consumer = await client.CreateConsumer(new ConsumerOptions
    {
        StationName = "<station-name>",
        ConsumerName = "<consumer-name>",
        ConsumerGroup = "<consumer-group-name>",
    }); 
       
}
catch (Exception ex)
{
    Console.Error.WriteLine("Exception: " + ex.Message);
    Console.Error.WriteLine(ex);
}
```

Note:
When consuming from a station with more than one partition, the consumer will consume messages in Round Robin fashion from the different partitions.

Use the Memphis CreateConsumer method to create a Consumer. It offeres some extra options that may be useful.

Here is an example on how to create a consumer with all of the default options:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var consumer = await memphisClient.CreateConsumer(new MemphisConsumerOptions
    {
        StationName = "MyStation",
        ConsumerName = "MyConsumer"
    });
}
catch (Exception ex)
{
    // handle exception
}handle exception
}
```

To create a consumer in a consumer group, add the ConsumerGroup MemphisConsumerOptions:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
var memphisClient = await MemphisClientFactory.CreateClient(options);

    var consumer = await memphisClient.CreateConsumer(new MemphisConsumerOptions
    {
        StationName = "MyStation",
        ConsumerName = "MyConsumer",
        ConsumerGroup = "MyConsumerGroup1"
    });
}
catch (Exception ex)
{
    // handle exception
}
```

When using Consumer.consume, the consumer will continue to consume in an infinite loop. To change the rate at which the consumer polls, change the PullIntervalMs parameter:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var consumer = await memphisClient.CreateConsumer(new MemphisConsumerOptions
    {
        StationName = "MyStation",
        ConsumerName = "MyConsumer",
        PullIntervalMs = 2_000
    });
}
catch (Exception ex)
{
    // handle exception
}
```

Every time the consumer polls, the consumer will try to take BatchSize number of elements from the station. However, sometimes there are not enough messages in the station for the consumer to consume a full batch. In this case, the consumer will continue to wait until either BatchSize messages are gathered or the time in milliseconds specified by BatchMaxTimeToWaitMs is reached. 

Here is an example of a consumer that will try to poll 100 messages every 10 seconds while waiting up to 15 seconds for all messages to reach the consumer.

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
var memphisClient = await MemphisClientFactory.CreateClient(options);

    var consumer = await memphisClient.CreateConsumer(new MemphisConsumerOptions
    {
        StationName = "MyStation",
        ConsumerName = "MyConsumer",
        PullIntervalMs = 10_000,
        BatchSize = 100,
        BatchMaxTimeToWaitMs = 15_000
    });
}
catch (Exception ex)
{
    // handle exception
}
```

The MaxMsgDeliveries parameter allows the user how many messages the consumer is able to consume before consuming more.

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var consumer = await memphisClient.CreateConsumer(new MemphisConsumerOptions
    {
        StationName = "MyStation",
        ConsumerName = "MyConsumer",
        PullIntervalMs = 10_000,
        BatchSize = 100,
        BatchMaxTimeToWaitMs = 15_000,
        MaxMsgDeliveries = 100
    });
}
catch (Exception ex)
{
    // handle exception
}
```

### Creating message handler for consuming a message

To configure message handler, use the `MessageReceived` event:

```c#
consumer.MessageReceived += (sender, args) =>
{
    if (args.Exception != null)
    {
        Console.Error.WriteLine(args.Exception);
        return;
    }

    foreach (var msg in args.MessageList)
    {
        //print message itself
        Console.WriteLine("Received data: " + Encoding.UTF8.GetString(msg.GetData()));


        // print message headers
        foreach (var headerKey in msg.GetHeaders().Keys)
        {
            Console.WriteLine(
                $"Header Key: {headerKey}, value: {msg.GetHeaders()[headerKey.ToString()]}");
        }

        Console.WriteLine("---------");
        msg.Ack();
    }
    Console.WriteLine("destroyed");
};
```

### Consuming a message

The consumer will try to fetch messages every _PullIntervalMs_ (that was given in Consumer's creation) and call the defined message handler.

```c#
 await consumer.ConsumeAsync();
```


#### Consumer schema deserialization
To get messages deserialized, use `msg.GetDeserializedData()` or  `msg.GetDeserializedData<T>()`.  

```csharp
consumer.MessageReceived += (sender, args) =>
{
    if (args.Exception != null)
    {
        Console.Error.WriteLine(args.Exception);
        return;
    }

    foreach (var msg in args.MessageList)
    {
        Console.WriteLine($"Received data: {msg.GetDeserializedData()}");
        msg.Ack();
    }
};
```

if you have ingested data into station in one format, afterwards you apply a schema on the station, the consumer won't deserialize the previously ingested data. For example, you have ingested string into the station and attached a protobuf schema on the station. In this case, consumer won't deserialize the string.

### Fetch a single batch of messages

```c#
client.FetchMessages(new FetchMessageOptions
{
    StationName= "<station-name>",
    ConsumerName= "<consumer-name>",
    ConsumerGroup= "<group-name>", // defaults to the consumer name.
    BatchSize= 10, // defaults to 10
    BatchMaxTimeToWaitMs= 5000, // defaults to 5000
    MaxAckTimeMs= 30000, // defaults to 30000
    MaxMsgDeliveries= 10, // defaults to 10
    StartConsumeFromSequence= 1, // start consuming from a specific sequence. defaults to 1
    LastMessages= -1 // consume the last N messages, defaults to -1 (all messages in the station)
});
```

### Fetch a single batch of messages after creating a consumer

`prefetch = true` will prefetch next batch of messages and save it in memory for future Fetch() request \
Note: Use a higher MaxAckTime as the messages will sit in a local cache for some time before processing

```C#
var messages = consumer.Fetch(
    batchSize: 10,
    prefetch: true
);
```

### Acknowledging a Message

Acknowledging a message indicates to the Memphis server to not re-send the same message again to the same consumer or consumers group.

```c#
msg.Ack();
```

### Delay the message after a given duration

Delay the message and tell Memphis server to re-send the same message again to the same consumer group.\
The message will be redelivered only in case `Consumer.MaxMsgDeliveries` is not reached yet.

```C#
msg.Delay(<delayMilliSeconds>);
```

### Get headers

Get headers per message

```c#
msg.GetHeaders()
```

### Destroying a Consumer

```c#
await consumer.DestroyAsync();
```

### Check if broker is connected

```c#
memphisClient.IsConnected();
```
