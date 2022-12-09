using System.Runtime.Serialization;

namespace Memphis.Client.Models.Request
{
    [DataContract]
    public sealed class CreateConsumerRequest
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
    }
}