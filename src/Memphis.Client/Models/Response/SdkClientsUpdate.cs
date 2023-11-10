#nullable disable

namespace Memphis.Client.Models.Response;

[DataContract]
internal class SdkClientsUpdate
{
    [DataMember(Name = "station_name")]
    public string StationName { get; set; }

    [DataMember(Name = "type")]
    public string Type { get; set; }

    [DataMember(Name = "update")]
    public bool? Update { get; set; }
}