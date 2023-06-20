using System.Runtime.Serialization;

namespace Memphis.Client.Models.Request;

#nullable disable

[DataContract]
internal class CreateSchemaRequest
{
    [DataMember(Name = "name")]
    public string Name { get; set; }
    [DataMember(Name = "type")]
    public string Type { get; set; }
    [DataMember(Name = "created_by_username")]
    public string CreatedByUsername { get; set; }
    [DataMember(Name = "schema_content")]
    public string SchemaContent { get; set; }
    [DataMember(Name = "message_struct_name")]
    public string MessageStructName { get; set; }
}
