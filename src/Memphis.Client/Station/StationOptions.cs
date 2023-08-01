#nullable disable

namespace Memphis.Client.Station
{
    public sealed class StationOptions
    {
        public string Name { get; set; }
        /// <summary>
        /// The retention type to use for the station. Valid values are:
        /// <list type="bullet">
        /// <item><description>message_age_sec</description></item>
        /// <item><description>messages</description></item>
        /// <item><description>bytes</description></item>
        /// <item><description>ack_based</description></item>
        /// </list>
        /// <description>
        /// Use the <see cref="RetentionTypes"/> class for the valid values.
        /// </description>
        /// </summary>
        public string RetentionType { get; set; } = RetentionTypes.MAX_MESSAGE_AGE_SECONDS;
        public int RetentionValue { get; set; } = 604_800;
        public string StorageType { get; set; } = StorageTypes.DISK;
        public int Replicas { get; set; } = 1;
        public int IdempotenceWindowMs { get; set; } = 120_000;
        public string SchemaName { get; set; } = string.Empty;
        public bool SendPoisonMessageToDls { get; set; } = false;
        public bool SendSchemaFailedMessageToDls { get; set; } = true;
        public bool TieredStorageEnabled { get; set; } = false;
    }

    public class RetentionTypes
    {
        public const string MAX_MESSAGE_AGE_SECONDS = "message_age_sec";
        public const string MESSAGES = "messages";
        public const string BYTES = "bytes";
        /// <summary>
        /// Retention based on message acks. This is for cloud users only.
        /// </summary>
        public const string ACK_BASED = "ack_based";
    }

    public class StorageTypes
    {
        public const string DISK = "file";
        public const string MEMORY = "memory";
    }
}