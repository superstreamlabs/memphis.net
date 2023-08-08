using System.Collections.Specialized;
using Memphis.Client.Station;

namespace Memphis.Client.IntegrationTests.Fixtures;

public class MemphisClientFixture
{
    internal readonly ClientOptions MemphisClientOptions;
    internal readonly StationOptions DefaultStationOptions;
    internal NameValueCollection CommonHeaders;

    public MemphisClientFixture()
    {
        MemphisClientOptions = MemphisClientFactory.GetDefaultOptions();
        MemphisClientOptions.Username = "root";
        MemphisClientOptions.Host = "localhost";
        MemphisClientOptions.Password = "memphis";
        
        DefaultStationOptions = new StationOptions
        {
            Name = "default",
            RetentionType = RetentionTypes.MAX_MESSAGE_AGE_SECONDS,
            RetentionValue = 604_800,
            StorageType = StorageTypes.DISK,
            Replicas = 1,
            IdempotenceWindowMs = 0,
            SendPoisonMessageToDls = true,
            SendSchemaFailedMessageToDls = true,
            PartitionsNumber = 3
        };

        CommonHeaders = new NameValueCollection();
        CommonHeaders.Add("key-1", "value-1");
    }
}