#nullable disable
namespace Memphis.Client.Models.Request;

[DataContract]
internal sealed class AttachSchemaRequest
{
    [DataMember(Name = "name")]
    public string SchemaName { get; set; }

    [DataMember(Name = "station_name")]
    public string StationName { get; set; }
    [DataMember(Name = "username")]
    public string UserName { get; set; }
}