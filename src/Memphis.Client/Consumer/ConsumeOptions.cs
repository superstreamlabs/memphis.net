namespace Memphis.Client.Consumer;

public sealed class ConsumeOptions
{
    public string PartitionKey { get; set; }
    /// <summary>
    /// The number of the partition to consume from. If not set, the default value is -1, and the consumer will consume from all partitions.
    /// </summary>
    public int PartitionNumber { get; set; } = -1;

    public ConsumeOptions()
    {
        PartitionKey = string.Empty;
    }
}
