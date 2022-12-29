using System;
using System.Collections.Concurrent;

namespace Memphis.Client.Validators
{
    internal abstract class SchemaValidatorBase<TSchema>
    {
        protected readonly ConcurrentDictionary<string, TSchema> _schemaCache;


        public SchemaValidatorBase()
        {
            this._schemaCache = new ConcurrentDictionary<string, TSchema>();
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
                var newSchema = Parse(schemaData);
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
            _schemaCache.TryRemove(schemaName, out TSchema schemaObj);
        }

        protected abstract TSchema Parse(string schemaData);
    }
}