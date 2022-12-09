using System;
using System.Security.Cryptography;
using System.Text;
using Memphis.Client.Exception;
using Memphis.Client.Helper;
using NATS.Client;
using NATS.Client.JetStream;

namespace Memphis.Client
{
    public class MemphisClientFactory
    {
        public static ClientOptions GetDefaultOptions()
        {
            return new ClientOptions();
        }

        public static MemphisClient CreateClient(ClientOptions opts)
        {
            //populate config options for broker
            var connectionId = MemphisUtil.GetUniqueKey(24);
            
            var brokerConnOptions = ConnectionFactory.GetDefaultOptions();
            brokerConnOptions.Servers = new[] { $"{MemphisClientFactory.normalizeHost(opts.Host)}:{opts.Port}" };
            brokerConnOptions.AllowReconnect = opts.Reconnect;
            brokerConnOptions.ReconnectWait = opts.MaxReconnectIntervalMs;
            brokerConnOptions.Token = opts.ConnectionToken;
            brokerConnOptions.Name = $"{connectionId}::{opts.Username}";
            brokerConnOptions.MaxPingsOut = 1;
            brokerConnOptions.Verbose = true;
            
            Console.WriteLine(brokerConnOptions.User);

            try
            {
                IConnection brokerConnection = new ConnectionFactory()
                    .CreateConnection(brokerConnOptions);
                IJetStream jetStreamContext = brokerConnection.CreateJetStreamContext();
            
                return new MemphisClient(
                    brokerConnOptions, brokerConnection,
                    jetStreamContext, connectionId);
            }
            catch (System.Exception e)
            {
                throw new MemphisConnectionException("error occured, when connecting memphis", e);
            }
        }
        
        private static string normalizeHost(string host)
        {
            if (host.StartsWith("http://"))
            {
                return host.Replace("http://", string.Empty);
            }
            
            if (host.StartsWith("https://"))
            {
                return host.Replace("https://", string.Empty);
            }

            return host;
        }
    }
}