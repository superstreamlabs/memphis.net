namespace Memphis.Client;

[DataContract]
internal class NackDlsMessage
{
    [DataMember(Name = "station_name")]
    public string StationName { get; set; } = null!;

    [DataMember(Name = "error")]
    public string Error { get; set; } = null!;
    
    [DataMember(Name = "partition")]
    public int Partition { get; set; }
    
    [DataMember(Name = "cg_name")]
    public string ConsumerGroupName { get; set; } = null!;

    [DataMember(Name = "seq")]
    public ulong StreamSequence { get; set; }
    
}
