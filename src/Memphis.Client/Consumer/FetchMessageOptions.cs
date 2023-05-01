#nullable disable 

namespace Memphis.Client.Consumer
{
    public sealed class FetchMessageOptions
    {
        public string ConsumerName { get; set; }
        public string StationName { get; set; }
        public string ConsumerGroup { get; set; }
        public int BatchSize { get; set; } = 10;
        public int BatchMaxTimeToWaitMs { get; set; } = 5_000;
        public int MaxAckTimeMs { get; set; } = 30_000;
        public int MaxMsgDeliveries { get; set; } = 10;
        public bool GenerateUniqueSuffix { get; set; } 
        public int StartConsumeFromSequence { get; set; } = 0;
        public int LastMessages { get; set; } = -1;
        public bool Prefetch { get; set; }
    }
}