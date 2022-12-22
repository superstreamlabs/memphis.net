using System.Threading.Tasks;

namespace Memphis.Client.Validators
{
    public interface ISchemaValidator
    {
        Task ValidateAsync(byte[] messageToValidate, string schemaAsStr);

        bool ParseAndStore(string schemeName, string schemaData);

        void RemoveSchema(string schemaName);
    }
}