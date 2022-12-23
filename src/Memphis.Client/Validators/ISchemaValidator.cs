using System.Threading.Tasks;

namespace Memphis.Client.Validators
{
    internal interface ISchemaValidator
    {
        Task ValidateAsync(byte[] messageToValidate, string schemaAsStr);

        bool ParseAndStore(string schemeName, string schemaData);

        void RemoveSchema(string schemaName);
    }
}