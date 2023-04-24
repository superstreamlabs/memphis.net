using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using Memphis.Client.Exception;

namespace Memphis.Client.Validators
{
    internal class GraphqlValidator : ISchemaValidator
    {
        private readonly IDocumentExecuter _documentExecutor;
        private readonly ConcurrentDictionary<string, ISchema> _schemaCache;


        public GraphqlValidator()
        {
            this._documentExecutor = new DocumentExecuter();
            this._schemaCache = new ConcurrentDictionary<string, ISchema>();
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

        public bool ParseAndStore(string schemeName, string schemaData)
        {
            if (string.IsNullOrEmpty(schemeName))
            {
                throw new ArgumentException($"Invalid value provided for {schemeName}");
            }

            if (string.IsNullOrEmpty(schemaData))
            {
                throw new ArgumentException($"Invalid value provided for {schemaData}");
            }

            try
            {
                var newSchema = Schema.For(schemaData);
                _schemaCache.AddOrUpdate(schemeName, newSchema, (key, oldVal) => newSchema);

                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        public void RemoveSchema(string schemaName)
        {
            _schemaCache.TryRemove(schemaName, out ISchema schemaObj);
        }
    }
}