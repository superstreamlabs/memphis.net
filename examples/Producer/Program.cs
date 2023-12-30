using Memphis.Client;
using System.Collections.Specialized;
using System.Text;
using System.Text.Json;

try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "aws-us-east-1.cloud.memphis.dev";
    options.AccountId = int.Parse(Environment.GetEnvironmentVariable("memphis_account_id"));
    options.Username = "test_user";
    options.Password = Environment.GetEnvironmentVariable("memphis_pass");

    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var producer = await memphisClient.CreateProducer(
        new Memphis.Client.Producer.MemphisProducerOptions
        {
            StationName = "test_station",
            ProducerName = "producer"
        });

    Message message = new()
    {
        Hello = "World!"
    };

    var headers = new NameValueCollection();

    for (int i = 0; i < 3; i++)
    {
        var msgBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        await producer.ProduceAsync(
          msgBytes,
          headers);
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