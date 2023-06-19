using System.Runtime.Serialization;

namespace Memphis.Client.Models.Response;

#nullable disable
[DataContract]
internal class CreateSchemaResponse
{
    [DataMember(Name = "error")]
    public string Error { get; set; }
}
