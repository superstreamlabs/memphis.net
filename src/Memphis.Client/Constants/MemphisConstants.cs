namespace Memphis.Client.Constants
{
    internal class MemphisStations
    {
        public const string MEMPHIS_PRODUCER_CREATIONS = "$memphis_producer_creations";
        public const string MEMPHIS_CONSUMER_CREATIONS = "$memphis_consumer_creations";
        public const string MEMPHIS_STATION_CREATIONS = "$memphis_station_creations";
        
        public const string MEMPHIS_PRODUCER_DESTRUCTIONS = "$memphis_producer_destructions";
        public const string MEMPHIS_CONSUMER_DESTRUCTIONS = "$memphis_consumer_destructions";

        public const string MEMPHIS_SCHEMA_ATTACHMENTS = "$memphis_schema_attachments";
        public const string MEMPHIS_SCHEMA_DETACHMENTS = "$memphis_schema_detachments";
        
        public const string MEMPHIS_NOTIFICATIONS = "$memphis_notifications";

    }

    internal class MemphisHeaders
    {
        public const string MESSAGE_ID = "msg-id";
        public const string MEMPHIS_PRODUCED_BY = "$memphis_producedBy";
        public const string MEMPHIS_CONNECTION_ID = "$memphis_connectionId";
    }
    
    internal class MemphisSubcriptions
    {
        public const string DLQ_PREFIX = "$memphis_dlq_";
    }
    internal class MemphisSubjects
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