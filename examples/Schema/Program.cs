using System;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using Memphis.Client;
using Memphis.Client.Producer;
using Memphis.Client.Constants;

namespace Schema
{
    class SchemaApp
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var options = MemphisClientFactory.GetDefaultOptions();
                options.Host = "<memphis-host>"
                options.Username = "<username>";
                options.Password = "<password>";
                // options.AccountId = <account-id>;
                // The AccountId field should be sent only on the cloud version of Memphis, otherwise it will be ignored.
                var client = await MemphisClientFactory.CreateClient(options);

                // Create schema
                var schemaName = "test_schema_person";
                await client.CreateSchema(
                    schemaName,
                    schemaType: MemphisSchemaTypes.JSON,
                    schemaFilePath: "person-schema.json");
                
                // Create station
                var stationName = "person_station";
                await client.CreateStation(stationName);

                // Enforce schema to station
                await client.EnforceSchema(stationName, schemaName);

                // Detach schema from station
                await client.DetachSchema(stationName);

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception: " + ex.Message);
                Console.Error.WriteLine(ex);
            }
        }
    }
}