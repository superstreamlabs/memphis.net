using System;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using Memphis.Client;

namespace Producer
{
    class ProducerApp
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var options = MemphisClientFactory.GetDefaultOptions();
                options.Host = "<memphis-host>";
                options.Username = "<application type username>";
                options.ConnectionToken = "<broker-token>";
                var client = MemphisClientFactory.CreateClient(options);

                var producer = await client.CreateProducer(
                    stationName: "<memphis-station-name>",
                    producerName: "<memphis-producer-name>",
                    generateRandomSuffix:true);

                var commonHeaders = new NameValueCollection();
                commonHeaders.Add("key-1", "value-1");

                for (int i = 0; i < 10_000000; i++)
                {
                    await Task.Delay(1_000);
                    var text = $"Message #{i}: Welcome to Memphis";
                    await producer.ProduceAsync(Encoding.UTF8.GetBytes(text), commonHeaders);
                    Console.WriteLine($"Message #{i} sent successfully");
                }

                await producer.DestroyAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception: " + ex.Message);
                Console.Error.WriteLine(ex);
            }
        }
    }
}