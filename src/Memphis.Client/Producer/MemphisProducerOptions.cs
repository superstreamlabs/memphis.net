using System;

#nullable disable

namespace Memphis.Client.Producer
{
    public class MemphisProducerOptions
    {
        public string StationName { get; set; }
        public string ProducerName { get; set; }
        
        [Obsolete("GenerateUniqueSuffix will be stopped to be supported after November 1'st, 2023.")]
        public bool GenerateUniqueSuffix { get; set; }
        
        public int MaxAckTimeMs { get; set; } = 30_000;
	}
}

