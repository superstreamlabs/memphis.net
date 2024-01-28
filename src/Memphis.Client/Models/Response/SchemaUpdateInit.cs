#nullable disable

namespace Memphis.Client.Models.Response;

[DataContract]
internal sealed class SchemaUpdateInit
{
    [DataMember(Name = "schema_name")]
    public string SchemaName { get; set; }

    [DataMember(Name = "active_version")]
    public ProducerSchemaUpdateVersion ActiveVersion { get; set; }

    [DataMember(Name = "type")]
    public string SchemaType { get; set; }
}