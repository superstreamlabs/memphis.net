#nullable disable

namespace Memphis.Client.Models.Request;

internal class ProducerDetails
{
    [JsonPropertyName( "name")]
    public string Name { get; set; }
    [JsonPropertyName( "connection_id")]
    public string ConnectionId { get; set; }
}

internal class MessagePayloadDls
{
    [JsonPropertyName( "size")]
    public int Size { get; set; }
    [JsonPropertyName( "data")]
    public string Data { get; set; }
    [JsonPropertyName( "headers")]
    public Dictionary<string, string> Headers { get; set; }
}

internal class DlsMessage
{
    [JsonPropertyName( "station_name")]
    public string StationName { get; set; }
    [JsonPropertyName( "producer")]
    public ProducerDetails Producer { get; set; }
    [JsonPropertyName( "message")]
    public MessagePayloadDls Message { get; set; }

    [JsonPropertyName( "validation_error")]
    public string ValidationError { get; set; }
}