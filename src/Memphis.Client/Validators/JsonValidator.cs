using System.Linq;
using NJsonSchema;

namespace Memphis.Client.Validators;

internal class JsonValidator : SchemaValidator<JsonSchema>, ISchemaValidator
{
    protected override JsonSchema Parse(string schemaData, string _)
    {
        return JsonSchema.FromJsonAsync(schemaData).GetAwaiter().GetResult();
    }

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
}