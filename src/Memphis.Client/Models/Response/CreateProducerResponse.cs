#nullable disable
namespace Memphis.Client.Models.Response;

internal sealed class PartitionsUpdate
{
    [JsonPropertyName( "partitions_list")]
    public int[] PartitionsList { get; set; }
}

internal sealed class CreateProducerResponse
{
    [JsonPropertyName( "schema_update")]
    public SchemaUpdateInit SchemaUpdate { get; set; }

    [JsonPropertyName( "error")]
    public string Error { get; set; }

    [JsonPropertyName( "send_notification")]
    public bool SendNotification { get; set; }

    [JsonPropertyName( "schemaverse_to_dls")]
    public bool SchemaVerseToDls { get; set; }

    [JsonPropertyName( "partitions_update")]
    public PartitionsUpdate PartitionsUpdate { get; set; }

    [JsonPropertyName( "station_version")]
    public int StationVersion { get; set; }

    [JsonPropertyName( "station_partitions_first_functions")]
    public Dictionary<int, int> StationPartitionsFirstFunctions { get; set; }

    public CreateProducerResponse()
    {
        StationPartitionsFirstFunctions = new();
    }
}