using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Memphis.Client.Exception;
using NJsonSchema;

namespace Memphis.Client.Validators
{
    internal class JsonValidator : SchemaValidatorBase<JsonSchema>, ISchemaValidator
    {
        protected override JsonSchema Parse(string schemaData)
        {
            return JsonSchema.FromSampleJson(schemaData);
        }

        public Task ValidateAsync(byte[] messageToValidate, string schemaAsStr)
        {
            if (_schemaCache.TryGetValue(schemaAsStr, out JsonSchema schemaObj))
            {
                var jsonMsg = Encoding.UTF8.GetString(messageToValidate);
                var errors = schemaObj.Validate(jsonMsg);

                if (errors.Any())
                {
                    var sb = new StringBuilder();
                    foreach (var error in errors)
                    {
                        sb.AppendLine(error.ToString());
                    }
                    
                    throw new MemphisSchemeValidationException(sb.ToString());
                }

                return Task.CompletedTask;
            }

            throw new MemphisSchemaValidationException($"Schema: {schemaAsStr} not found in local cache");
        }
    }
}