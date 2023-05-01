using System.Runtime.Serialization;

#nullable disable

namespace Memphis.Client.Models.Request
{
    [DataContract]
    internal sealed class CreateStationRequest
    {
        [DataMember(Name = "name")]
        public string StationName { get; set; }
        
        [DataMember(Name = "retention_type")]
        public string RetentionType { get; set; }
        
        [DataMember(Name = "retention_value")]
        public int RetentionValue { get; set; }
        
        [DataMember(Name = "storage_type")]
        public string StorageType { get; set; }
        
        [DataMember(Name = "replicas")]
        public int Replicas { get; set; }

        [DataMember(Name = "idempotency_windows_in_ms")]
        public int IdempotencyWindowsInMs { get; set; }
        
        [DataMember(Name = "schema_name")]
        public string SchemaName { get; set; }

        [DataMember(Name = "dls_configuration")]
        public DlsConfiguration DlsConfiguration { get; set; }

        [DataMember(Name = "username")]
        public string UserName { get; set; }

        [DataMember(Name = "tiered_storage_enabled")]
        public bool TieredStorageEnabled { get; set; }
    }

    [DataContract]
    internal sealed class DlsConfiguration
    {
        [DataMember(Name = "poison")]
        public bool Poison { get; set; }
        
        [DataMember(Name = "SchemaVerse")]
        public bool SchemaVerse { get; set; }
    }
}