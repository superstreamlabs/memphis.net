using System.Collections.Generic;
using System.Runtime.Serialization;

#nullable disable

namespace Memphis.Client.Models.Request
{
    [DataContract]
    internal class ProducerDetails
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "connection_id")]
        public string ConnectionId { get; set; }
    }

    [DataContract]
    internal class MessagePayloadDls
    {
        [DataMember(Name = "size")]
        public int Size { get; set; }
        [DataMember(Name = "data")]
        public string Data { get; set; }
        [DataMember(Name = "headers")]
        public Dictionary<string, string> Headers { get; set; }
    }

    [DataContract]
    internal class DlsMessage
    {
        [DataMember(Name = "station_name")]
        public string StationName { get; set; }
        [DataMember(Name = "producer")]
        public ProducerDetails Producer { get; set; }
        [DataMember(Name = "message")]
        public MessagePayloadDls Message { get; set; }

        [DataMember(Name = "validation_error")]
        public string ValidationError { get; set; }
    }
}