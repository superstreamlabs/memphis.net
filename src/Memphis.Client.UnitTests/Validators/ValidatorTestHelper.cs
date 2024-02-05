using Memphis.Client.Constants;
using Memphis.Client.Models.Response;

namespace Memphis.Client.UnitTests.Validators;

internal class ValidatorTestHelper
{
    public static SchemaUpdateInit GetSchemaUpdateInit(
        string schemaName,
        string schema,
        string schemaType
    )
    {
        return new SchemaUpdateInit
        {
            SchemaName = schemaName,
            ActiveVersion = new ProducerSchemaUpdateVersion
            {
                VersionNumber = 1,
                Descriptor = string.Empty,
                Content = schema,
                MessageStructName = string.Empty
            },
            SchemaType = schemaType
        };
    }
}