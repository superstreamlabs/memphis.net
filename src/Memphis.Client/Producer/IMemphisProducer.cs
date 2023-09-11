using System;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Memphis.Client.Producer;

public interface IMemphisProducer
{
    /// <summary>
    /// Produce messages into station
    /// </summary>
    /// <param name="message">message to produce</param>
    /// <param name="headers">headers used to send data in the form of key and value</param>
    /// <param name="ackWaitMs">duration of time in milliseconds for acknowledgement</param>
    /// <param name="messageId">Message ID - for idempotent message production</param>
    /// <param name="asyncProduceAck">Whether to wait for ack before returning</param>
    /// <returns></returns>
    public Task ProduceAsync(byte[] message, NameValueCollection headers, int ackWaitMs = 15_000,
        string? messageId = default, bool asyncProduceAck = true, string partitionKey = "");

    /// <summary>
    /// Produce messages into station
    /// </summary>
    /// <param name="message">message to produce</param>
    /// <param name="headers">headers used to send data in the form of key and value</param>
    /// <param name="ackWaitMs">duration of time in milliseconds for acknowledgement</param>
    /// <param name="messageId">Message ID - for idempotent message production</param>
    /// <param name="asyncProduceAck">Whether to wait for ack before returning</param>
    /// <returns></returns>
    public Task ProduceAsync<T>(T message, NameValueCollection headers, int ackWaitMs = 15_000,
        string? messageId = default, bool asyncProduceAck = true, string partitionKey = "");

    /// <summary>
    /// Destroy producer
    /// </summary>
    /// <returns></returns>
    /// <exception cref="MemphisException"></exception>
    Task DestroyAsync();

}