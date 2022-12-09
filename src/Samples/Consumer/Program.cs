using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Memphis.Client;
using Memphis.Client.Consumer;
using Memphis.Client.Core;

namespace Consumer
{
    class ConsumerApp
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var options = MemphisClientFactory.GetDefaultOptions();
                options.Host = "localhost";
                options.Username = "dotnetapp";
                options.ConnectionToken = "memphis";
                var client = MemphisClientFactory.CreateClient(options);

                var consumer = await client.CreateConsumer(new ConsumerOptions
                {
                    StationName = "test-station",
                    ConsumerName = "dotnetappconsumer",
                    ConsumerGroup = "test",
                });

                var cancellationTokenSource = new CancellationTokenSource();

                EventHandler<MemphisMessageHandlerEventArgs> msgHandler = (sender, args) =>
                {
                    if (args.Exception != null)
                    {
                        Console.Error.WriteLine(args.Exception);
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
                };

                await consumer.Consume(msgHandler, msgHandler, cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception: " + ex.Message);
                Console.Error.WriteLine(ex);
            }
        }
    }
}