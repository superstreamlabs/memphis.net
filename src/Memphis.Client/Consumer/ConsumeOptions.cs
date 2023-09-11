namespace Memphis.Client.Consumer;

public sealed class ConsumeOptions
{
    public string PartitionKey { get; set; }

    public ConsumeOptions()
    {
        PartitionKey = string.Empty;
    }
}
