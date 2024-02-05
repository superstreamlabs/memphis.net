#nullable disable
namespace Memphis.Client.Models.Request;

internal sealed class AttachSchemaRequest
{
    [JsonPropertyName( "name")]
    public string SchemaName { get; set; }

    [JsonPropertyName( "station_name")]
    public string StationName { get; set; }
    [JsonPropertyName( "username")]
    public string UserName { get; set; }
}