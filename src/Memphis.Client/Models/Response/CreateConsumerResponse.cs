namespace Memphis.Client.Models.Response;

#nullable disable

internal sealed class CreateConsumerResponse
{
    [JsonPropertyName("partitions_update")]
    public PartitionsUpdate PartitionsUpdate { get; set; }

    [JsonPropertyName("error")]
    public string Error { get; set; }

    [JsonPropertyName("schema_update")]
    public SchemaUpdateInit SchemaUpdate { get; set; }
}
