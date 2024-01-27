namespace Memphis.Client.Validators;

internal interface ISchemaValidator
{
    Task ValidateAsync(byte[] messageToValidate, string schemaAsStr);

    bool AddOrUpdateSchema(SchemaUpdateInit schemaUpdate);

    void RemoveSchema(string schemaName);
}