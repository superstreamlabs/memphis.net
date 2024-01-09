namespace Memphis.Client;

public static class MemphisExceptions{

    public static readonly MemphisConnectionException DeadConnectionException = new MemphisConnectionException("Connection to the broker is dead");

    public static readonly MemphisConnectionException UnableToDealyDLSException = new MemphisConnectionException("Unable to delay DLS message");

    public static readonly MemphisException EmptySchemaNameException = new MemphisException("Schema name can not be empty");

    public static readonly MemphisException SchemaNameTooLongException = new MemphisException("Schema name should be under 128 characters");

    public static readonly MemphisException InvalidSchemaNameException = new MemphisException("Only alphanumeric and the '_', '-', '.' characters are allowed in schema name");

    public static readonly MemphisException InvalidSchemaStartEndCharsException = new MemphisException("Schema name can not start or end with non alphanumeric character");

    public static readonly MemphisException EmptySchemaTypeException = new MemphisException("Schema type can not be empty");

    public static readonly MemphisException UnsupportedSchemaTypeException = new MemphisException("Unsupported schema type, the supported schema types are: json, graphql, protobuf, avro\"");

    public static readonly MemphisException SchemaUpdateSubscriptionFailedException = new MemphisException("Unable to add subscription of schema updates for station");

    public static readonly MemphisException InvalidConnectionTypeException = new MemphisException("You have to connect with one of the following methods: connection token / password");

    public static readonly MemphisException StationUnreachableException = new MemphisException("Station unreachable");

    public static readonly MemphisException BothPartitionNumAndKeyException = new MemphisException("PartitionKey and PartitionNumber can not be set at the same time");

    public static readonly MemphisMessageIdException EmptyMessageIDException = new MemphisMessageIdException("Message ID cannot be empty");

    public static readonly MemphisException UnsupportedOSException = new MemphisException("Unsupported OS");

    public static MemphisException FailedToConnectException(System.Exception e)
    {
        return new MemphisConnectionException("error occurred, when connecting memphis", e);
    }

    public static MemphisException FailedToCreateConsumerException(System.Exception e)
    {
        return new MemphisException("Failed to create memphis consumer", e);
    }

    public static MemphisException FailedToCreateProducerException(System.Exception e)
    {
        return new MemphisException("Failed to create memphis producer", e);
    }


    public static MemphisException FailedToCreateStationException(System.Exception e)
    {
        return new MemphisException("Failed to create memphis station", e);
    }

    public static MemphisException FailedToAttachSchemaException(System.Exception e)
    {
        return new MemphisException("Failed to attach schema to station", e);
    }

    public static MemphisException SchemaDoesNotExistException(string path)
    {
        return new MemphisException("Schema file does not exist", new FileNotFoundException(path));
    }

    public static MemphisException FailedToDestroyConsumerException(System.Exception e)
    {
        return new MemphisException("Failed to destroy consumer", e);
    }

    public static MemphisConnectionException AckFailedException(System.Exception e)
    {
        return new MemphisConnectionException("Unable to ack message", e);
    }

}