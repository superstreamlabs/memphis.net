#nullable disable

namespace Memphis.Client.Models.Request;

internal sealed class CreateProducerRequest
{
    [JsonPropertyName( "name")]
    public string ProducerName { get; set; }

    [JsonPropertyName( "station_name")]
    public string StationName { get; set; }

    [JsonPropertyName( "connection_id")]
    public string ConnectionId { get; set; }

    [JsonPropertyName( "producer_type")]
    public string ProducerType { get; set; }

    [JsonPropertyName( "req_version")]
    public int RequestVersion { get; set; }

    [JsonPropertyName( "username")]
    public string UserName { get; set; }

    [JsonPropertyName( "app_id")]
    public string ApplicationId { get; set; }

    [JsonPropertyName( "sdk_lang")]
    public string SdkLang { get; set; }
}