using System.Runtime.Serialization;

#nullable disable

namespace Memphis.Client.Models.Request
{
    [DataContract]
    internal sealed class CreateConsumerRequest
    {
        [DataMember(Name = "name")]
        public string ConsumerName { get; set; }
        
        [DataMember(Name = "station_name")]
        public string StationName { get; set; }
        
        [DataMember(Name = "connection_id")]
        public string ConnectionId { get; set; }
        
        [DataMember(Name = "consumer_type")]
        public string ConsumerType { get; set; }
        
        [DataMember(Name = "consumers_group")]
        public string ConsumerGroup { get; set; }
        
        [DataMember(Name = "max_ack_time_ms")]
        public int MaxAckTimeMs { get; set; }
        
        [DataMember(Name = "max_msg_deliveries")]
        public int MaxMsgCountForDelivery { get; set; }
        
        [DataMember(Name = "username")]
        public string UserName { get; set; }
        
        [DataMember(Name = "start_consume_from_sequence")]
        public int StartConsumeFromSequence { get; set; }
        
        [DataMember(Name = "last_messages")]
        public int LastMessages { get; set; }

        [DataMember(Name = "req_version")]
        public int RequestVersion { get; set; }
    }
}