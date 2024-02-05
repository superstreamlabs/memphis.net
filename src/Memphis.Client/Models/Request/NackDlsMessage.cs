namespace Memphis.Client;

internal class NackDlsMessage
{
    [JsonPropertyName( "station_name")]
    public string StationName { get; set; } = null!;

    [JsonPropertyName( "error")]
    public string Error { get; set; } = null!;
    
    [JsonPropertyName( "partition")]
    public int Partition { get; set; }
    
    [JsonPropertyName( "cg_name")]
    public string ConsumerGroupName { get; set; } = null!;

    [JsonPropertyName( "seq")]
    public ulong StreamSequence { get; set; }
    
}
