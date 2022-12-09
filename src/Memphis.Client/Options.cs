namespace Memphis.Client
{
    public sealed class ClientOptions
    {
        public string Host { get; set; }
        public string Username { get; set; }
        public string ConnectionToken { get; set; }
        public int Port { get; set; } = 6666;
        public bool Reconnect { get; set; } = true;
        public int MaxReconnect { get; set; } = 10;
        public int MaxReconnectIntervalMs { get; set; } = 1500;
        public int TimeoutMs { get; set; } = 15_000;
    }
}