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
                options.Username = "<application type username";
                options.ConnectionToken = "<broker-token>";
                var client = MemphisClientFactory.CreateClient(options);

                var producer = await client.CreateProducer("<station-name>", "<producer-name>");

                var commonHeaders = new NameValueCollection();
                commonHeaders.Add("key-1", "value-1");

                for (int i = 0; i < 5; i++)
                {
                    var text = $"Message #{i}: Welcome to Memphis";
                    await producer.ProduceAsync(Encoding.UTF8.GetBytes(text), commonHeaders);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception: " + ex.Message);
                Console.Error.WriteLine(ex);
            }
        }
    }
}