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

        [DataMember(Name = "connection_id")]
        public string ConnectionId { get; set; }

        [DataMember(Name = "username")]
        public string Username { get; set; }
    }
}