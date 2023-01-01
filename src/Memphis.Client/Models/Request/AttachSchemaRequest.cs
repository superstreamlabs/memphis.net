using System.Runtime.Serialization;

namespace Memphis.Client.Models.Request
{
    [DataContract]
    internal sealed class AttachSchemaRequest
    {
        [DataMember(Name = "name")]
        public string SchemaName { get; set; }
        
        [DataMember(Name = "station_name")]
        public string StationName { get; set; }
    }
}