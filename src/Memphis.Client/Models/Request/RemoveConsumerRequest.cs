#nullable disable

namespace Memphis.Client.Models.Request;

internal sealed class RemoveConsumerRequest
{
    [JsonPropertyName( "name")]
    public string ConsumerName { get; set; }

    [JsonPropertyName( "station_name")]
    public string StationName { get; set; }

    [JsonPropertyName( "connection_id")]
    public string ConnectionId { get; set; }

    [JsonPropertyName( "username")]
    public string Username { get; set; }

    [JsonPropertyName( "req_version")]
    public int RequestVersion { get; set; }

}