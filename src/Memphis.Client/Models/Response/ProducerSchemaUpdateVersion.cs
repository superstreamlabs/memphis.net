using System.Runtime.Serialization;

namespace Memphis.Client.Models.Response
{
    [DataContract]
    internal sealed class ProducerSchemaUpdateVersion
    {
        [DataMember(Name = "version_number")]
        public string VersionNumber { get; set; }
        
        [DataMember(Name = "descriptor")]
        public string Descriptor { get; set; }
        
        [DataMember(Name = "schema_content")]
        public string Content { get; set; }
        
        [DataMember(Name = "message_struct_name")]
        public string MessageStructName { get; set; }
    }
}