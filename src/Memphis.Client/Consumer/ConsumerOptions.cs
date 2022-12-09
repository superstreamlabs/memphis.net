namespace Memphis.Client.Consumer
{
    public class ConsumerOptions
    {
        public string StationName { get; set; }
        public string ConsumerName { get; set; }
        public string ConsumerGroup { get; set; } = string.Empty;
        public int PullIntervalMs { get; set; } = 1_000;
        public int BatchSize { get; set; } = 10;
        public int BatchMaxTimeToWaitMs { get; set; } = 5_000;
        public int MaxAckTimeMs { get; set; } = 30_000;
        public int MaxMsdgDeliveries { get; set; } = 10;
        public bool GenerateRandomSuffix { get; set; } = false;
    }
}