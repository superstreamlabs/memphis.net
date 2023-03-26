using System;
using System.Text;
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
                options.Host = "<memphis-host>";
                options.Username = "<application type username>";
                options.ConnectionToken = "<broker-token>";
                var client = MemphisClientFactory.CreateClient(options);

                var consumer = await client.CreateConsumer(new MemphisConsumerOptions
                {
                    StationName = "<station-name>",
                    ConsumerName = "<consumer-name>",
                    ConsumerGroup = "<consumer-group-name>",
                });

                EventHandler<MemphisMessageHandlerEventArgs> msgHandler = (sender, args) =>
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

                consumer.ConsumeAsync(
                    msgCallbackHandler:msgHandler,
                    dlqCallbackHandler:msgHandler);

                // Wait 10 seconds, consumer starts to consume, if you need block main thread use await keyword.
                await Task.Delay(10_000);
                await consumer.DestroyAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception: " + ex.Message);
                Console.Error.WriteLine(ex);
            }
        }
    }
}