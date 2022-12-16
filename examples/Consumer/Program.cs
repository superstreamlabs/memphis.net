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

                var consumer = await client.CreateConsumer(new ConsumerOptions
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
                };

                await consumer.ConsumeAsync(
                    msgCallbackHandler:msgHandler,
                    dlqCallbackHandler:msgHandler);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception: " + ex.Message);
                Console.Error.WriteLine(ex);
            }
        }
    }
}