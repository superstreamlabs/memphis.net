#nullable disable

namespace Memphis.Client.Models.Request;

internal class RemoveStationRequest
{
    [JsonPropertyName( "station_name")]
    public string StationName { get; set; }
    [JsonPropertyName( "username")]
    public string Username { get; set; }
}