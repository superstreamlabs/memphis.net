using NJsonSchema;

namespace Memphis.Client.Validators;

internal class JsonValidator : SchemaValidator<JsonSchema>, ISchemaValidator
{

    public Task ValidateAsync(byte[] messageToValidate, string schemaAsStr)
    {
        if (!_schemaCache.TryGetValue(schemaAsStr, out var schemaObj))
            throw new MemphisSchemaValidationException($"Schema: {schemaAsStr} not found in local cache");
        try
        {
            var jsonMsg = Encoding.UTF8.GetString(messageToValidate);
            var errors = schemaObj.Validate(jsonMsg);

            if (!errors.Any()) return Task.CompletedTask;
            var sb = new StringBuilder();
            foreach (var error in errors)
            {
                sb.AppendLine(error.ToString());
            }

            throw new MemphisSchemaValidationException($"Schema validation has failed: \n {sb.ToString()}");
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
            var schemaData = schemaUpdate.ActiveVersion.Content;
            var newSchema = Parse(schemaData, schemeName);
            _schemaCache.AddOrUpdate(schemeName, newSchema, (key, oldVal) => newSchema);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private JsonSchema Parse(string schemaData, string _)
    {
        return JsonSchema.FromJsonAsync(schemaData).GetAwaiter().GetResult();
    }
}