#nullable disable

using System.Text.Json.Serialization;

namespace Memphis.Client.Models.Request;

internal sealed class CreateStationRequest
{
    [JsonPropertyName("name")]
    public string StationName { get; set; }

    [JsonPropertyName("retention_type")]
    public string RetentionType { get; set; }

    [JsonPropertyName("retention_value")]
    public int RetentionValue { get; set; }

    [JsonPropertyName("storage_type")]
    public string StorageType { get; set; }

    [JsonPropertyName("replicas")]
    public int Replicas { get; set; }

    [JsonPropertyName("idempotency_windows_in_ms")]
    public int IdempotencyWindowsInMs { get; set; }

    [JsonPropertyName("schema_name")]
    public string SchemaName { get; set; }

    [JsonPropertyName("dls_configuration")]
    public DlsConfiguration DlsConfiguration { get; set; }

    [JsonPropertyName("username")]
    public string UserName { get; set; }

    [JsonPropertyName("tiered_storage_enabled")]
    public bool TieredStorageEnabled { get; set; }

    [JsonPropertyName("partitions_number")]
    public int PartitionsNumber { get; set; }

    [JsonPropertyName("dls_station")]
    public string DlsStation { get; set; }
}


internal sealed class DlsConfiguration
{
    [JsonPropertyName("poison")]
    public bool Poison { get; set; }

    [JsonPropertyName("SchemaVerse")]
    public bool SchemaVerse { get; set; }
}