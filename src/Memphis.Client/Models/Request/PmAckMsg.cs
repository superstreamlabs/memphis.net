#nullable disable

namespace Memphis.Client.Models.Request;

internal sealed class PmAckMsg
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("cg_name")]
    public string ConsumerGroupName { get; set; }
}