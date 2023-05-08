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
                TimeoutMs = 15_000,
                AccountId = 1
            };
        }

        /// <summary>
        /// Create Memphis Client
        /// </summary>
        /// <param name="opts">Client Options used to customize behavior of client used to connect Memphis</param>
        /// <param name="isAccountIdIgnored">If true, account id will be ignored. This param is added for backward compatibility.</param>
        /// <returns>An <see cref="MemphisClient"/> object connected to the Memphis server.</returns>
        public static async Task<MemphisClient> CreateClient(ClientOptions opts,
            bool isAccountIdIgnored = false,
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
                brokerConnOptions.User = isAccountIdIgnored ? opts.Username : $"{opts.Username}${opts.AccountId}";
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
                IConnection brokerConnection = new ConnectionFactory()
                    .CreateConnection(brokerConnOptions);
                IJetStream jetStreamContext = brokerConnection.CreateJetStreamContext();
                MemphisClient client = new(
                    brokerConnOptions, brokerConnection,
                    jetStreamContext, connectionId);
                await client.ListenForSdkClientUpdate();
                await client.ConfigureTenantName(opts.AccountId, cancellationToken);
                return client;
            }
            catch (System.Exception e)
            {
                if (!isAccountIdIgnored)
                {
                    return await CreateClient(opts, true, cancellationToken);
                }
                throw new MemphisConnectionException("error occurred, when connecting memphis", e);
            }
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