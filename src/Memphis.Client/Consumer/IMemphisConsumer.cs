namespace Memphis.Client.Consumer;

public interface IMemphisConsumer : IDisposable
{
    /// <summary>
    /// Event raised when a message is received
    /// </summary>
    event EventHandler<MemphisMessageHandlerEventArgs> MessageReceived;

    /// <summary>
    /// Event raised when a DLS message is received
    /// </summary>
    event EventHandler<MemphisMessageHandlerEventArgs> DlsMessageReceived;

    /// <summary>
    /// ConsumeAsync messages
    /// </summary>
    /// <returns></returns>
    Task ConsumeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// ConsumeAsync messages
    /// </summary>
    /// <param name="options">Consume options</param>
    /// <param name="cancellationToken">token used to cancel operation by Consumer</param>
    /// <returns></returns>
    Task ConsumeAsync(ConsumeOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch a batch of messages
    /// </summary>
    /// <param name="batchSize">the number of messages to fetch</param>
    /// <param name="prefetch">if true, the messages are prefetched</param>
    /// <param name="cancellationToken">token used to cancel operation by Consumer</param>
    /// <returns>A batch of messages</returns>
    IEnumerable<MemphisMessage> Fetch(int batchSize, bool prefetch);

    /// <summary>
    /// Fetch messages from station
    /// </summary>
    /// <param name="options">Fetch options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="MemphisMessage"/></returns>
    Task<IEnumerable<MemphisMessage>> FetchMessages(
        FetchMessageOptions options,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Destroy the consumer
    /// </summary>
    /// <returns></returns>
    Task DestroyAsync(int timeoutRetry = 5);
}