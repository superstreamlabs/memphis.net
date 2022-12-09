namespace Memphis.Client
{
    public sealed class ClientOptions
    {
        public string Host { get; set; }
        public string Username { get; set; }
        public string ConnectionToken { get; set; }
        public int Port { get; set; }
        public bool Reconnect { get; set; }
        public int MaxReconnect { get; set; }
        public int MaxReconnectIntervalMs { get; set; }
        public int TimeoutMs { get; set; }
    }
}