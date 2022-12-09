using System.Runtime.Serialization;
//using System.Text.Json.Serialization;
//using Newtonsoft.Json;

namespace Memphis.Client.Models.Request
{
    [DataContract]
    public sealed class CreateProducerRequest
    {
        [DataMember(Name = "name")]
        // ////[JsonPropertyName("name")]
        // //[JsonProperty("name")]
        public string ProducerName { get; set; }
        
        [DataMember(Name = "station_name")]
        // ////[JsonPropertyName("station_name")]
        // //[JsonProperty("name")]
        public string StationName { get; set; }
        
        [DataMember(Name = "connection_id")]
        ////[JsonPropertyName("connection_id")]
        //[JsonProperty("name")]
        public string ConnectionId { get; set; }
        
        [DataMember(Name = "producer_type")]
        ////[JsonPropertyName("producer_type")]
        //[JsonProperty("name")]
        public string ProducerType { get; set; }
        
        [DataMember(Name = "req_version")]
        ////[JsonPropertyName("req_version")]
        //[JsonProperty("name")]
        public int RequestVersion { get; set; }
    }
}