using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

#nullable disable 

namespace Memphis.Client
{
    public sealed class TlsOptions
    {
        public TlsOptions(string fileName)
            => (FileName) = (fileName);

        public TlsOptions(string fileName, string password) : this(fileName)
            => (Password) = (password);

        public TlsOptions(X509Certificate2 certificate)
            => (Certificate) = (certificate);

        public X509Certificate2 Certificate { get; set; }
        public string FileName { get; set; }
        public string Password { get; set; }
        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }
    }

    public sealed class ClientOptions
    {
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ConnectionToken { get; set; }
        public int Port { get; set; }
        public bool Reconnect { get; set; }
        public int MaxReconnect { get; set; }
        public int MaxReconnectIntervalMs { get; set; }
        public int TimeoutMs { get; set; }
        public TlsOptions Tls { get; set; }
        public int AccountId { get; set; }
    }
}