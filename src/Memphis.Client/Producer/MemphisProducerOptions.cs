using System;
namespace Memphis.Client.Producer
{
    public class MemphisProducerOptions
    {
        public string StationName { get; set; }
        public string ProducerName { get; set; }
        public bool GenerateUniqueSuffix { get; set; }
        public int MaxAckTimeMs { get; set; } = 30_000;
	}
}

