using System.Runtime.Serialization;

#nullable disable

namespace Memphis.Client.Models.Request
{
    [DataContract]
    internal sealed class RemoveProducerRequest
    {
        [DataMember(Name = "name")]
        public string ProducerName { get; set; }

        [DataMember(Name = "station_name")]
        public string StationName { get; set; }

        [DataMember(Name = "tenant_name")]
        public string TenantName { get; set; }
    }
}