using System.Runtime.Serialization;

#nullable disable

namespace Memphis.Client.Models.Request
{
    [DataContract]
    internal sealed class NotificationRequest
    {
        [DataMember(Name = "title", IsRequired = true)]
        public string Title { get; set; }
        
        [DataMember(Name = "msg", IsRequired = true)]
        public string Message { get; set; }
        
        [DataMember(Name = "type", IsRequired = true)]
        public string Type { get; set; }

        [DataMember(Name = "code")]
        public string Code { get; set; }

    }
}