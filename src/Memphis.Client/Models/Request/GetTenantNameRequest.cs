using System.Runtime.Serialization;

#nullable disable

namespace Memphis.Client.Models.Request
{
    [DataContract]
    public class GetTenantNameRequest
    {
        [DataMember(Name = "tenant_id")]
        public int TenantId { get; set; }
    }
}