#nullable disable

namespace Memphis.Client.Models.Request;

[DataContract]
internal sealed class CreateProducerRequest
{
    [DataMember(Name = "name")]
    public string ProducerName { get; set; }

    [DataMember(Name = "station_name")]
    public string StationName { get; set; }

    [DataMember(Name = "connection_id")]
    public string ConnectionId { get; set; }

    [DataMember(Name = "producer_type")]
    public string ProducerType { get; set; }

    [DataMember(Name = "req_version")]
    public int RequestVersion { get; set; }

    [DataMember(Name = "username")]
    public string UserName { get; set; }

    [DataMember(Name = "app_id")]
    public string ApplicationId { get; set; }

    [DataMember(Name = "sdk_lang")]
    public string SdkLang { get; set; }
}