using System.Threading.Tasks;
using SolTechnology.Avro;

using Memphis.Client.Exception;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Memphis.Client.Validators;

internal class AnotherUserModel
{
    [DataMember(Name = "name")]
    public string Name { get; set; } = null!;

    [DataMember(Name = "age")]
    public long Age { get; set; }
}

internal class AvroValidator : SchemaValidator<string>, ISchemaValidator
{
    public Task ValidateAsync(byte[] messageToValidate, string schemaKey)
    {
        if (!_schemaCache.TryGetValue(schemaKey, out var schemaObj))
            throw new MemphisSchemaValidationException($"Schema: {schemaKey} not found in local cache");
        try
        {
            _ = AvroConvert.Avro2Json(messageToValidate, schemaObj);
            return Task.CompletedTask;
        }
        catch (System.Exception exception)
        {
            throw new MemphisSchemaValidationException($"Schema validation has failed: \n {exception.Message}", exception);
        }
    }

    protected override string Parse(string schemaData, string schemaName)
    {
        try
        {
            _ = AvroConvert.GenerateModel(schemaData);
            return schemaData;
        }
        catch (System.Exception exception)
        {
            throw new MemphisException($"Schema parsing has failed: \n {exception.Message}", exception);
        }
    }
}