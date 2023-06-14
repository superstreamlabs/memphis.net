using System;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Memphis.Client.Exception;
using NATS.Client;
using NATS.Client.JetStream;

namespace Memphis.Client
{
    public static class MemphisClientFactory
    {
        public static ClientOptions GetDefaultOptions()
        {
            return new ClientOptions
            {
                Port = 6666,
                Reconnect = true,
                MaxReconnect = 10,
                MaxReconnectIntervalMs = 1_500,
                TimeoutMs = (int)TimeSpan.FromSeconds(2).TotalMilliseconds,
                AccountId = 1
            };
        }

        /// <summary>
        /// Create Memphis Client
        /// </summary>
        /// <param name="opts">Client Options used to customize behavior of client used to connect Memphis</param>
        /// <returns>An <see cref="MemphisClient"/> object connected to the Memphis server.</returns>
        public static async Task<MemphisClient> CreateClient(ClientOptions opts,
            CancellationToken cancellationToken = default)
        {
            if (XNOR(string.IsNullOrWhiteSpace(opts.ConnectionToken),
               string.IsNullOrWhiteSpace(opts.Password)))
                throw new MemphisException("You have to connect with one of the following methods: connection token / password");

            var connectionId = Guid.NewGuid().ToString();

            var brokerConnOptions = ConnectionFactory.GetDefaultOptions();
            brokerConnOptions.Servers = new[] { $"{NormalizeHost(opts.Host)}:{opts.Port}" };
            brokerConnOptions.AllowReconnect = opts.Reconnect;
            brokerConnOptions.ReconnectWait = opts.MaxReconnectIntervalMs;
            brokerConnOptions.Name = $"{connectionId}::{opts.Username}";
            brokerConnOptions.Timeout = opts.TimeoutMs;
            brokerConnOptions.Verbose = true;

            if (!string.IsNullOrWhiteSpace(opts.ConnectionToken))
            {
                brokerConnOptions.Token = opts.ConnectionToken;
            }
            else
            {
                brokerConnOptions.User = $"{opts.Username}${opts.AccountId}";
                brokerConnOptions.Password = opts.Password;
            }


            if (opts.Tls != null)
            {
                brokerConnOptions.Secure = true;
                brokerConnOptions.CheckCertificateRevocation = true;
                if (opts.Tls.Certificate is not null)
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

                if (opts.Tls.RemoteCertificateValidationCallback is not null)
                {
                    brokerConnOptions.TLSRemoteCertificationValidationCallback = opts.Tls.RemoteCertificateValidationCallback;
                }
            }

            try
            {

                SuppressDefaultEventHandlerLogs(brokerConnOptions);

                IConnection brokerConnection = await EstablishBrokerManagerConnection(brokerConnOptions, cancellationToken);
                IJetStream jetStreamContext = brokerConnection.CreateJetStreamContext();
                MemphisClient client = new(
                    brokerConnOptions, brokerConnection,
                    jetStreamContext, connectionId);
                await client.ListenForSdkClientUpdate();
                return client;
            }
            catch (System.Exception e)
            {
                throw new MemphisConnectionException("error occurred, when connecting memphis", e);
            }


            void SuppressDefaultEventHandlerLogs(Options options)
            {
                options.ClosedEventHandler += (_, _) => { };
                options.ServerDiscoveredEventHandler += (_, _) => { };
                options.DisconnectedEventHandler += (_, _) => { };
                options.ReconnectedEventHandler += (_, _) => { };
                options.LameDuckModeEventHandler += (_, _) => { };
                options.AsyncErrorEventHandler += (_, _) => { };
                options.HeartbeatAlarmEventHandler += (_, _) => { };
                options.UnhandledStatusEventHandler += (_, _) => { };
                options.FlowControlProcessedEventHandler += (_, _) => { };
            }
        }

        /// <summary>
        /// This method is used to connect to Memphis Broker. 
        /// It attempts to establish connection using accountId, and if it fails, it tries to connect using username(with out accountId).
        /// </summary>
        /// <param name="brokerOptions">Broker Options used to customize behavior of client used to connect Memphis</param>
        /// <returns>An <see cref="IConnection"/> object connected to the Memphis server.</returns>
        private static async Task<IConnection> EstablishBrokerManagerConnection(Options brokerOptions, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(brokerOptions.User))
            {
                await DelayLocalConnection(brokerOptions.Servers);
                return new ConnectionFactory()
                    .CreateConnection(brokerOptions);
            }

            try
            {
                return new ConnectionFactory()
                    .CreateConnection(brokerOptions);
            }
            catch
            {
                var pattern = @"(?<username>[^$]*)(?<separator>\$)(?<accountId>.+)";
                if (Regex.Match(brokerOptions.User, pattern) is { Success: true } match)
                {
                    await DelayLocalConnection(brokerOptions.Servers);
                    brokerOptions.User = match.Groups["username"].Value;
                    return new ConnectionFactory()
                        .CreateConnection(brokerOptions);
                }
                throw;
            }
        }

        /// <summary>
        /// Delay local connection for handling bad quality networks like port fwd
        /// </summary>
        /// <param name="servers">List of servers</param>
        /// <returns>Task</returns>
        private static async Task DelayLocalConnection(string[] servers)
        {
            if (servers is { Length: > 0 } && IsLocalConnection(servers[0]))
                await Task.Delay((int)TimeSpan.FromSeconds(1).TotalMilliseconds);
        }

        /// <summary>
        /// Check if connection is local
        /// </summary>
        /// <param name="host">Host</param>
        /// <returns>True if connection is local, otherwise false</returns>
        private static bool IsLocalConnection(string host)
        {
            return
                !string.IsNullOrWhiteSpace(host) &&
                host.Contains("localhost");
        }

        /// <summary>
        /// XNOR operator
        /// </summary>
        /// <param name="a">First boolean value</param>
        /// <param name="b">Second boolean value</param>
        /// <returns>True if both values are equal, otherwise false</returns>
        private static bool XNOR(bool a, bool b)
            => a == b;

        /// <summary>
        /// Normalize host
        /// </summary>
        /// <remark>
        /// Remove http:// or https:// from host
        /// </remark>
        /// <param name="host">Host</param>
        /// <returns>Normalized host</returns>
        internal static string NormalizeHost(string host)
        {
            return Regex.Replace(host, "^http(s?)://", string.Empty);
        }
    }
}