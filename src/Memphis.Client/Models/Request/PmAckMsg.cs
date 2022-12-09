using System.Runtime.Serialization;

namespace Memphis.Client.Models.Request
{
    [DataContract]
    public class PmAckMsg
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        
        [DataMember(Name = "cg_name")]
        public string ConsumerGroupName { get; set; }
    }
}