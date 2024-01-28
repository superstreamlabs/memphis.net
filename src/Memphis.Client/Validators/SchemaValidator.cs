namespace Memphis.Client.Validators;

internal abstract class SchemaValidator<TSchema>
{
    protected readonly ConcurrentDictionary<string, TSchema> _schemaCache;

    public SchemaValidator()
    {
        _schemaCache = new ConcurrentDictionary<string, TSchema>();
    }

    public void RemoveSchema(string schemaName)
    {
        _schemaCache.TryRemove(schemaName, out TSchema _);
    }

    protected bool IsSchemaUpdateValid(SchemaUpdateInit schemaUpdate)
    {
        if (schemaUpdate is null ||
            schemaUpdate is { ActiveVersion: null })
            return false;

        if (string.IsNullOrEmpty(schemaUpdate.ActiveVersion.Content) ||
            string.IsNullOrEmpty(schemaUpdate.SchemaName))
            return false;

        return true;
    }
}