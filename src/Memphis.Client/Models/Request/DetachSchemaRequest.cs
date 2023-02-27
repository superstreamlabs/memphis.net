using System.Runtime.Serialization;

namespace Memphis.Client.Models.Request
{
    [DataContract]
    internal sealed class DetachSchemaRequest
    {
        [DataMember(Name = "station_name")]
        public string StationName { get; set; }
        [DataMember(Name = "user_name")]
        public string UserName { get; set; }
    }
}