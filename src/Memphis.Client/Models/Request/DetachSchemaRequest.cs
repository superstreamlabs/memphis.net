using System.Runtime.Serialization;

namespace Memphis.Client.Models.Request
{
    [DataContract]
    internal sealed class DetachSchemaRequest
    {
        [DataMember(Name = "station_name")]
        public string StationName { get; set; }
    }
}