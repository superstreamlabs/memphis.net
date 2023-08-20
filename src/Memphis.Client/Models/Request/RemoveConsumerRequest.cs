using System.Runtime.Serialization;

#nullable disable

namespace Memphis.Client.Models.Request
{
    [DataContract]
    internal sealed class RemoveConsumerRequest
    {
        [DataMember(Name = "name")]
        public string ConsumerName { get; set; }

        [DataMember(Name = "station_name")]
        public string StationName { get; set; }

        [DataMember(Name = "connection_id")]
        public string ConnectionId { get; set; }

        [DataMember(Name = "username")]
        public string Username { get; set; }

        [DataMember(Name = "req_version")]
        public int RequestVersion { get; set; }

    }
}