#nullable disable

namespace Memphis.Client.Models.Request;

internal sealed class NotificationRequest
{
    [JsonPropertyName( "title")]
    public string Title { get; set; }
    
    [JsonPropertyName( "msg")]
    public string Message { get; set; }
    
    [JsonPropertyName( "type")]
    public string Type { get; set; }

    [JsonPropertyName( "code")]
    public string Code { get; set; }

}