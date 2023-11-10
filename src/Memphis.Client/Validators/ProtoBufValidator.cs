namespace Memphis.Client.Validators;

#nullable disable
internal class ProtoBufSchema
{
    public ProtoBufSchema(string name, string activeVersion)
    {
        SchemaName = name;
        ActiveSchemaVersion = activeVersion;
    }

    public string ActiveSchemaVersion { get; set; }
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
                base64Data: Convert.ToBase64String(messageToValidate), 
                activeSchemaVersionBase64: Convert.ToBase64String(Encoding.UTF8.GetBytes(protoBufSchema.ActiveSchemaVersion)), 
                schemaName: protoBufSchema.SchemaName);
            if(result.HasError)
                throw new MemphisSchemaValidationException($"Schema validation has failed: \n {result.Error}");

        }
        catch (System.Exception ex)
        {
            throw new MemphisSchemaValidationException($"Schema validation has failed: \n {ex.Message}", ex);
        }
    }

    protected override ProtoBufSchema Parse(string schemaData, string schemaName)
    {
        return new(name: schemaName, activeVersion: schemaData);
    }
}