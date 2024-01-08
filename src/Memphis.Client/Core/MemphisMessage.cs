using Memphis.Client.Helper;
using Memphis.Client.Models.Request;

namespace Memphis.Client.Core;

public sealed class MemphisMessage
{
    private readonly Msg _msg;
    private readonly MemphisClient _memphisClient;
    private readonly string _consumerGroup;
    private readonly int _macAckTimeMs;
    private readonly string _internalStationName;

    public MemphisMessage(
        Msg msgItem,
        MemphisClient memphisClient,
        string consumerGroup,
        int macAckTimeMs,
        string internalStationName)
    {
        this._msg = msgItem;
        this._memphisClient = memphisClient;
        this._consumerGroup = consumerGroup;
        this._macAckTimeMs = macAckTimeMs;
        this._internalStationName = internalStationName;
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

            throw new MemphisConnectionException("Unable to ack message", e);
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

        throw new MemphisConnectionException("Unable to delay DLS message");
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