using System.Runtime.Serialization;

namespace Memphis.Client.Models.Response
{
    [DataContract]
    public sealed class CreateProducerResponse
    {
        [DataMember(Name = "schema_update")]
        public ProducerSchemaUpdateInit SchemaUpdate { get; set; }
        
        [DataMember(Name = "error")]
        public string Error { get; set; }
    }
}