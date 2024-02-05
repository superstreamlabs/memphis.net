namespace Memphis.Client.Models.Request;

#nullable disable

internal class CreateSchemaRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("created_by_username")]
    public string CreatedByUsername { get; set; }
    [JsonPropertyName("schema_content")]
    public string SchemaContent { get; set; }
    [JsonPropertyName("message_struct_name")]
    public string MessageStructName { get; set; }
}
