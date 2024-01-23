namespace Memphis.Client.Core;

public sealed class MemphisMessage
{
    private readonly Msg _msg;
    private readonly MemphisClient _memphisClient;
    private readonly string _consumerGroup;
    private readonly int _macAckTimeMs;
    private readonly string _internalStationName;
    private readonly int _partitionNumber;

    private bool _isMessageInDls => _partitionNumber < -1;

    public MemphisMessage(
        Msg msgItem,
        MemphisClient memphisClient,
        string consumerGroup,
        int macAckTimeMs,
        string internalStationName,
        int partitionNumber
    )
    {
        _msg = msgItem;
        _memphisClient = memphisClient;
        _consumerGroup = consumerGroup;
        _macAckTimeMs = macAckTimeMs;
        _internalStationName = internalStationName;
        _partitionNumber = partitionNumber;
    }

    public void Ack()
    {
        try
        {
            this._msg.Ack();
        }
        catch (System.Exception e)
        {
            if (_msg.Header["$memphis_pm_id"] != null)
            {
                var msgToAckModel = new PmAckMsg
                {
                    Id = _msg.Header["$memphis_pm_id"],
                    ConsumerGroupName = _consumerGroup,
                };
                var msgToAckJson = JsonSerDes.PrepareJsonString<PmAckMsg>(msgToAckModel);

                byte[] msgToAckBytes = Encoding.UTF8.GetBytes(msgToAckJson);
                _memphisClient.BrokerConnection.Publish(
                    MemphisSubjects.PM_RESEND_ACK_SUBJ, msgToAckBytes);
            }

            throw MemphisExceptions.AckFailedException(e);
        }
    }

    /// <summary>
    ///   Nack message - not ack for a message, meaning that the message will be redelivered again to the same consumers group without waiting to its ack wait time.
    /// </summary>
    /// <exception cref="MemphisException">Throws when unable to nack message</exception>
    public void Nack()
    {
        try
        {
            _msg.Nak();
        }
        catch (System.Exception e)
        {
            throw MemphisExceptions.NackFailedException(e);
        }
    }

    /// <summary>
    ///  Dead letter message - Sending the message to the dead-letter station (DLS). the broker won't resend the message again to the same consumers group and will place the message inside the dead-letter station (DLS) with the given reason.
    ///  The message will still be available to other consumer groups
    /// </summary>
    ///  <param name="reason">Reason for dead lettering the message</param>
    ///  <exception cref="MemphisException">Throws when unable to send message dead letter</exception>
    public void DeadLetter(string reason)
    {
        try
        {
            if (_isMessageInDls)
                return;
            _msg.Term();
            var metadata = _msg.MetaData;
            var nackDlsMessage = new NackDlsMessage
            {
                StationName = _internalStationName,
                Error = reason,
                Partition = _partitionNumber,
                ConsumerGroupName = _consumerGroup,
                StreamSequence = metadata.StreamSequence,
            };
            _memphisClient.BrokerConnection.Publish(
                MemphisSubjects.NACKED_DLS,
                Encoding.UTF8.GetBytes(JsonSerDes.PrepareJsonString<NackDlsMessage>(nackDlsMessage))
            );
        }
        catch (System.Exception e)
        {
            throw MemphisExceptions.DeadLetterFailed(e);
        }
    }

    public byte[] GetData()
    {
        return _msg.Data;
    }

    public object? GetDeserializedData()
    {
        byte[] message = _msg.Data;
        var schemaType = _memphisClient.GetStationSchemaType(_internalStationName);
        if (string.IsNullOrWhiteSpace(schemaType))
            return message;

        return MessageSerializer.Deserialize<object>(message, schemaType);
    }

    public T? GetDeseriliazedData<T>() where T : class
    {
        return GetDeserializedData() as T;
    }

    public MsgHeader GetHeaders()
    {
        return _msg.Header;
    }

    /// <summary>
    ///    Delay message for a given time.
    /// </summary>
    /// <param name="delayMilliSeconds">Delay time in milliseconds</param>
    /// <exception cref="MemphisConnectionException">Throws when unable to delay message</exception>
    public void Delay(long delayMilliSeconds)
    {
        var headers = GetHeaders();
        if (TryGetHeaderValue("$memphis_pm_id", out string _))
        {
            _msg.NakWithDelay(delayMilliSeconds);
            return;
        }

        if (TryGetHeaderValue("$memphis_pm_cg_name", out string _))
        {
            _msg.NakWithDelay(delayMilliSeconds);
            return;
        }

        throw MemphisExceptions.UnableToDealyDLSException;
    }

    /// <summary>
    /// gets header value from message header
    /// </summary>
    /// <param name="key">header key</param>
    /// <param name="value">header value</param>
    /// <returns>true if header exists, false otherwise</returns>

    private bool TryGetHeaderValue(string key, out string value)
    {
        try
        {
            value = _msg.Header[key];
            return true;
        }
        catch
        {
            value = string.Empty;
            return false;
        }
    }

    public DateTime GetTimeSent()
    {
        return _msg.MetaData.Timestamp;
    }

    public ulong GetSequence()
    {
        return _msg.MetaData.StreamSequence;
    }
}