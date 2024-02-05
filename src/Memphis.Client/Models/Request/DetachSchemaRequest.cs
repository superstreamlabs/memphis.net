#nullable disable

namespace Memphis.Client.Models.Request;

internal sealed class DetachSchemaRequest
{
    [JsonPropertyName("station_name")]
    public string StationName { get; set; }
    [JsonPropertyName("username")]
    public string UserName { get; set; }
}