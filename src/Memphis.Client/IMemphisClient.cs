using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Memphis.Client.Consumer;
using Memphis.Client.Core;
using Memphis.Client.Producer;
using Memphis.Client.Station;

namespace Memphis.Client;

public interface IMemphisClient : IDisposable
{
    /// <summary>
    /// Create Producer for station 
    /// </summary>
    /// <param name="producerOptions">Producer options</param>
    /// <returns>An <see cref="MemphisProducer"/> object connected to the station to produce data</returns>
    Task<MemphisProducer> CreateProducer(MemphisProducerOptions producerOptions);

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
    /// Produce a message to station
    /// </summary>
    /// <param name="options">Producer options</param>
    /// <param name="message">Message to produce</param>
    /// <param name="headers">Message headers</param>
    /// <param name="messageId">Message id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    Task ProduceAsync(
        MemphisProducerOptions options,
        byte[] message,
        NameValueCollection headers = default,
        string messageId = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Produce a message to station
    /// </summary>
    /// <param name="options">Producer options</param>
    /// <param name="message">Message to produce</param>
    /// <param name="headers">Message headers</param>
    /// <param name="messageId">Message id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    Task ProduceAsync<T>(
        MemphisProducerOptions options,
        T message,
        NameValueCollection headers = default,
        string messageId = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create Consumer for station
    /// </summary>
    /// <param name="consumerOptions">Consumer options</param>
    /// <returns>An <see cref="MemphisConsumer"/> object connected to the station to consume data</returns>
    Task<MemphisConsumer> CreateConsumer(MemphisConsumerOptions consumerOptions);

    /// <summary>
    /// Create a station
    /// </summary>
    /// <param name="stationOptions">Station options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An <see cref="MemphisStation"/> object connected to the station</returns>
    Task<MemphisStation> CreateStation(StationOptions stationOptions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a station
    /// </summary>
    /// <param name="stationName">Station name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An <see cref="MemphisStation"/> object connected to the station</returns>
    Task<MemphisStation> CreateStation(string stationName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attach Schema to an existing station
    /// </summary>
    /// <param name="stationName">station name</param>
    /// <param name="schemaName">schema name</param>
    /// <returns></returns>
    Task AttachSchema(string stationName, string schemaName);

    /// <summary>
    /// Detach Schema from an existing station
    /// </summary>
    /// <param name="stationName">station name</param>
    /// <returns></returns>
    Task DetachSchema(string stationName);

    /// <summary>
    /// Create a new consumer
    /// </summary>
    /// <param name="fetchMessageOptions">Fetch message options</param>
    /// <returns>MemphisConsumer</returns>
    Task<MemphisConsumer> CreateConsumer(FetchMessageOptions fetchMessageOptions);

    
}