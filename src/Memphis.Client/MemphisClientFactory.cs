using System.Security.Cryptography.X509Certificates;
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
            return new ClientOptions
            {
                Port = 6666,
                Reconnect = true,
                MaxReconnect = 10,
                MaxReconnectIntervalMs = 1_500,
                TimeoutMs = 15_000,
            };
        }


        /// <summary>
        /// Create Memphis Client
        /// </summary>
        /// <param name="opts">Client Options used to customize behaviour of client used to connect Memmphis</param>
        /// <returns>An <see cref="MemphisClient"/> object connected to the Memphis server.</returns>
        public static MemphisClient CreateClient(ClientOptions opts)
        {
            var connectionId = MemphisUtil.GetUniqueKey(24);

            var brokerConnOptions = ConnectionFactory.GetDefaultOptions();
            brokerConnOptions.Servers = new[] { $"{normalizeHost(opts.Host)}:{opts.Port}" };
            brokerConnOptions.AllowReconnect = opts.Reconnect;
            brokerConnOptions.ReconnectWait = opts.MaxReconnectIntervalMs;
            brokerConnOptions.Token = opts.ConnectionToken;
            brokerConnOptions.Name = $"{connectionId}::{opts.Username}";
            brokerConnOptions.User = opts.Username;
            brokerConnOptions.Verbose = true;

            if (opts.Tls != null)
            {
                brokerConnOptions.Secure = true;
                brokerConnOptions.CheckCertificateRevocation = true;
                if (opts.Tls.Certificate != null)
                {
                    brokerConnOptions.AddCertificate(opts.Tls.Certificate);
                }
                else if (!string.IsNullOrWhiteSpace(opts.Tls.FileName))
                {
                    if (!string.IsNullOrWhiteSpace(opts.Tls.Password))
                    {
                        brokerConnOptions.AddCertificate(new X509Certificate2(opts.Tls.FileName, opts.Tls.Password));
                    }
                    else
                    {
                        brokerConnOptions.AddCertificate(opts.Tls.FileName);
                    }
                }
               
                if(opts.Tls.RemoteCertificateValidationCallback != null)
                {
                    brokerConnOptions.TLSRemoteCertificationValidationCallback = opts.Tls.RemoteCertificateValidationCallback;   
                }
            }

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