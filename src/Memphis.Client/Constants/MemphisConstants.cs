namespace Memphis.Client.Constants
{
    public class MemphisStations
    {
        public const string MEMPHIS_PRODUCER_CREATIONS = "$memphis_producer_creations";
        public const string MEMPHIS_CONSUMER_CREATIONS = "$memphis_consumer_creations";
        public const string MEMPHIS_STATION_CREATIONS = "$memphis_station_creations";
    }

    public class MemphisHeaders
    {
        public const string MESSAGE_ID = "msg-id";
        public const string MEMPHIS_PRODUCED_BY = "$memphis_producedBy";
        public const string MEMPHIS_CONNECTION_ID = "$memphis_connectionId";
    }
    
    public class MemphisSubcriptions
    {
        public const string DLQ_PREFIX = "$memphis_dlq_";
    }
    public class MemphisSubjects
    {
        public const string PM_RESEND_ACK_SUBJ = "$memphis_pm_acks";
        public const string MEMPHIS_SCHEMA_UPDATE = "$memphis_schema_updates_";
    }

    public static class MemphisSchemaTypes
    {
        public const string NONE = "";
        public const string JSON = "json";
        public const string GRAPH_QL = "graphql";
        public const string PROTO_BUF = "protobuf";
    }
}