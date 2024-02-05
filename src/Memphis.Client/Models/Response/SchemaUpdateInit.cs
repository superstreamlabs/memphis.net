#nullable disable

namespace Memphis.Client.Models.Response;

internal sealed class SchemaUpdateInit
{
    [JsonPropertyName("schema_name")]
    public string SchemaName { get; set; }

    [JsonPropertyName("active_version")]
    public ProducerSchemaUpdateVersion ActiveVersion { get; set; }

    [JsonPropertyName("type")]
    public string SchemaType { get; set; }
}