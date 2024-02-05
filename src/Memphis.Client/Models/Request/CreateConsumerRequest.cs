#nullable disable

namespace Memphis.Client.Models.Request;

internal sealed class CreateConsumerRequest
{
    [JsonPropertyName( "name")]
    public string ConsumerName { get; set; }
    
    [JsonPropertyName( "station_name")]
    public string StationName { get; set; }
    
    [JsonPropertyName( "connection_id")]
    public string ConnectionId { get; set; }
    
    [JsonPropertyName( "consumer_type")]
    public string ConsumerType { get; set; }
    
    [JsonPropertyName( "consumers_group")]
    public string ConsumerGroup { get; set; }
    
    [JsonPropertyName( "max_ack_time_ms")]
    public int MaxAckTimeMs { get; set; }
    
    [JsonPropertyName( "max_msg_deliveries")]
    public int MaxMsgCountForDelivery { get; set; }
    
    [JsonPropertyName( "username")]
    public string UserName { get; set; }
    
    [JsonPropertyName( "start_consume_from_sequence")]
    public int StartConsumeFromSequence { get; set; }
    
    [JsonPropertyName( "last_messages")]
    public int LastMessages { get; set; }

    [JsonPropertyName( "req_version")]
    public int RequestVersion { get; set; }

    [JsonPropertyName( "app_id")]
    public string ApplicationId { get; set; }

    [JsonPropertyName( "sdk_lang")]
    public string SdkLang { get; set; }
}