using Memphis.Client;
using Memphis.Client.Core;
using System.Text.Json;

try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "<memphis-host>";
    options.AccountId = <memphis-accountId>;
    options.Username = "<memphis-username>";
    options.Password = <memphis-password>;

    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var consumer = await memphisClient.CreateConsumer(
       new Memphis.Client.Consumer.MemphisConsumerOptions
       {
           StationName = "<station-name>",
           ConsumerName = "<consumer-name>"
       });

    var messages = consumer.Fetch(3, false);

    foreach (MemphisMessage message in messages)
    {
        var messageData = message.GetData();
        var messageOBJ = JsonSerializer.Deserialize<Message>(messageData);

        // Do something with the message here
        Console.WriteLine(JsonSerializer.Serialize(messageOBJ));

        message.Ack();
    }

    memphisClient.Dispose();

}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
}

public class Message
{
    public string Hello { get; set; }
}