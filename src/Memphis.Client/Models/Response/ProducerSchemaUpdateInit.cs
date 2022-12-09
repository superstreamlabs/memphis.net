using System.Runtime.Serialization;

namespace Memphis.Client.Models.Response
{
    [DataContract]
    public sealed class ProducerSchemaUpdateInit
    {
        [DataMember(Name = "schema_name")]
        public string SchemaName { get; set; }

        [DataMember(Name = "active_version")]
        public ProducerSchemaUpdateVersion ActiveVersion { get; set; }

        [DataMember(Name = "type")]
        public string SchemaType { get; set; }
    }
}