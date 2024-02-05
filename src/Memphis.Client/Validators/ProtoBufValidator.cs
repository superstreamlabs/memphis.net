
namespace Memphis.Client.Validators;

#nullable disable
internal class ProtoBufSchema
{
    public ProtoBufSchema(string name, string activeVersionBase64)
    {
        SchemaName = name;
        ActiveSchemaVersionBase64 = activeVersionBase64;
    }

    public string ActiveSchemaVersionBase64 { get; set; }
    public string SchemaName { get; set; }
}

#nullable enable
internal class ProtoBufValidator : SchemaValidator<ProtoBufSchema>, ISchemaValidator
{

    public async Task ValidateAsync(byte[] messageToValidate, string schemaAsStr)
    {
        if (!_schemaCache.TryGetValue(schemaAsStr, out var protoBufSchema))
            throw new MemphisSchemaValidationException($"Schema: {schemaAsStr} not found in local cache");

        try
        {
            var result = await ProtoBufEval.ProtoBufValidator.Validate(
                proto64: Convert.ToBase64String(messageToValidate),
                activeSchemaVersionBase64: protoBufSchema.ActiveSchemaVersionBase64,
                schemaName: protoBufSchema.SchemaName);
            if (result.HasError)
                throw new MemphisSchemaValidationException($"Schema validation has failed: \n {result.Error}");

        }
        catch (System.Exception ex)
        {
            throw new MemphisSchemaValidationException($"Schema validation has failed: \n {ex.Message}", ex);
        }
    }

    public bool AddOrUpdateSchema(SchemaUpdateInit schemaUpdate)
    {
        if (!IsSchemaUpdateValid(schemaUpdate))
            return false;

        try
        {
            var schemeName = schemaUpdate.SchemaName;
            var newSchema = Parse(schemaUpdate.ActiveVersion, schemeName);
            _schemaCache.AddOrUpdate(schemeName, newSchema, (key, oldVal) => newSchema);
            return true;
        }
        catch
        {
            return false;
        }

    }

    private ProtoBufSchema Parse(ProducerSchemaUpdateVersion activeVersion, string schemaName)
    {
        var avj = JsonSerializer.Serialize(new
        {
            version_number = Convert.ToInt32(activeVersion.VersionNumber),
            descriptor = activeVersion.Descriptor,
            schema_content = activeVersion.Content,
            message_struct_name = activeVersion.MessageStructName
        });   

        var av64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(avj));

        return new(name: schemaName, activeVersionBase64: av64);
    }
}