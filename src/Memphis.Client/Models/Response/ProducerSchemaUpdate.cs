#nullable disable

namespace Memphis.Client.Models.Response;

internal sealed class ProducerSchemaUpdate
{
    [JsonPropertyName( "UpdateType")] 
    public string UpdateType { get; set; }

    [JsonPropertyName( "init")] 
    public SchemaUpdateInit Init { get; set; }
}

internal static class ProducerSchemaUpdateType
{
    public const int SCHEMA_UPDATE_TYPE_INIT = 1;
    public const int SCHEMA_UPDATE_TYPE_DROP = 2;
}