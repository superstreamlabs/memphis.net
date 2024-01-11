#nullable disable

namespace Memphis.Client.Consumer;

public sealed class MemphisConsumerOptions
{
    private int _batchMaxTimeToWaitMs = 100;

    public string StationName { get; set; }
    public string ConsumerName { get; set; }
    public string ConsumerGroup { get; set; } = string.Empty;
    public int PullIntervalMs { get; set; } = 1_000;
    public int BatchSize { get; set; } = 10;
    /// <summary>
    ///    The maximum time to wait for a batch message to be consumed in milliseconds.
    ///    The default value is 5000 (5 seconds). 
    ///    The lowest value is 1000 (1 second), and if it is set a value lower than 1 second, it will be ignored.
    /// </summary>
    public int BatchMaxTimeToWaitMs
    {
        get => _batchMaxTimeToWaitMs;
        set => _batchMaxTimeToWaitMs =  (value < 100) ? 100 : value;
    }
    public int MaxAckTimeMs { get; set; } = 30_000;
    public int MaxMsgDeliveries { get; set; } = 2;

    [Obsolete("GenerateUniqueSuffix will be stopped to be supported after November 1'st, 2023.")]
    public bool GenerateUniqueSuffix { get; set; } = false;

    public int StartConsumeFromSequence { get; set; } = 1;
    public int LastMessages { get; set; } = -1;

    internal string RealName { get; set; }
}