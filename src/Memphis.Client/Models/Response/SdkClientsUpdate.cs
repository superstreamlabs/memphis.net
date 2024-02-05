#nullable disable

namespace Memphis.Client.Models.Response;

internal class SdkClientsUpdate
{
    [JsonPropertyName("station_name")]
    public string StationName { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("update")]
    public bool? Update { get; set; }
}