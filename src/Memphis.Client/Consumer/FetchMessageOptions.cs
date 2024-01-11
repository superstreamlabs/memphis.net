#nullable disable 

namespace Memphis.Client.Consumer;

public sealed class FetchMessageOptions
{
    private int _batchMaxTimeToWaitMs = 100;

    public string ConsumerName { get; set; }
    public string StationName { get; set; }
    public string ConsumerGroup { get; set; }
    public int BatchSize { get; set; } = 10;

    /// <summary>
    ///    The maximum time to wait for a batch message to be consumed in milliseconds.
    ///    The default value is 5000 (5 seconds). 
    ///    The lowest value is 1000 (1 second), and if it is set a value lower than 1 second, it will be ignored.
    /// </summary>
    public int BatchMaxTimeToWaitMs
    {
        get => _batchMaxTimeToWaitMs;
        set =>_batchMaxTimeToWaitMs = (value < 100) ? 100 : value;
    }

    public int MaxAckTimeMs { get; set; } = 30_000;
    public int MaxMsgDeliveries { get; set; } = 2;

    [Obsolete("GenerateUniqueSuffix will be stopped to be supported after November 1'st, 2023.")]
    public bool GenerateUniqueSuffix { get; set; } 
    public int StartConsumeFromSequence { get; set; } = 1;
    public int LastMessages { get; set; } = -1;
    public bool Prefetch { get; set; }

    public string PartitionKey { get; set; } = string.Empty;
    public int PartitionNumber { get; set; } = -1;
}