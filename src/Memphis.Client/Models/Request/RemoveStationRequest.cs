using System.Runtime.Serialization;

#nullable disable

namespace Memphis.Client.Models.Request
{
    [DataContract]
    internal class RemoveStationRequest
    {
        [DataMember(Name = "station_name")]
        public string StationName { get; set; }
        [DataMember(Name = "username")]
        public string Username { get; set; }
        
        [DataMember(Name = "tenant_name")]
        public string TenantName { get; set; }
    }
}