#nullable disable

namespace Memphis.Client.Models.Response;

internal sealed class ProducerSchemaUpdateVersion
{
    [JsonPropertyName("version_number")]
    public int VersionNumber { get; set; }

    [JsonPropertyName("descriptor")]
    public string Descriptor { get; set; }

    [JsonPropertyName("schema_content")]
    public string Content { get; set; }

    [JsonPropertyName("message_struct_name")]
    public string MessageStructName { get; set; }
}