using GraphQL;
using GraphQL.Types;

namespace Memphis.Client.Validators;

internal class GraphqlValidator : SchemaValidator<ISchema>, ISchemaValidator
{
    private readonly IDocumentExecuter _documentExecutor;

    public GraphqlValidator()
    {
        _documentExecutor = new DocumentExecuter();
    }

    public async Task ValidateAsync(byte[] messageToValidate, string schemaName)
    {
        if (_schemaCache.TryGetValue(schemaName, out ISchema schemaObj))
        {
            var queryToValidate = Encoding.UTF8.GetString(messageToValidate);
            var execResult = await _documentExecutor.ExecuteAsync(_ =>
            {
                _.Schema = schemaObj;
                _.Query = queryToValidate;
            });

            if (execResult.Errors?.Count > 0)
            {
                var errMsg = string.Join("; ", execResult.Errors?
                    .Select(err => $"Code: {err.Code}, Message: {err.Message}"));

                throw new MemphisSchemaValidationException($"Schema validation has failed: \n {errMsg}");
            }

            return;
        }

        throw new MemphisSchemaValidationException($"Schema: {schemaName} not found in local cache");
    }

    public bool AddOrUpdateSchema(SchemaUpdateInit schemaUpdate)
    {
        if (!IsSchemaUpdateValid(schemaUpdate))
            return false;

        try
        {
            var schemeName = schemaUpdate.SchemaName;
            var schemaData = schemaUpdate.ActiveVersion.Content;
            var newSchema = Schema.For(schemaData);
            _schemaCache.AddOrUpdate(schemeName, newSchema, (key, oldVal) => newSchema);
            return true;
        }
        catch
        {
            return false;
        }
    }
}