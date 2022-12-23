using System.Runtime.Serialization;

namespace Memphis.Client.Models.Response
{
    [DataContract]
    internal sealed class ProducerSchemaUpdateInit
    {
        [DataMember(Name = "schema_name")]
        public string SchemaName { get; set; }

        [DataMember(Name = "active_version")]
        public ProducerSchemaUpdateVersion ActiveVersion { get; set; }

        [DataMember(Name = "type")]
        public string SchemaType { get; set; }
        
        internal static class SchemaTypes
        {
            public const string PROTOBUF = "protobuf";
            public const string JSON = "json";
            public const string GRAPHQL = "graphql";
        }
    }
}