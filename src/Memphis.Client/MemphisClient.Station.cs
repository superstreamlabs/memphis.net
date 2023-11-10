using Memphis.Client.Helper;

namespace Memphis.Client;

public partial class MemphisClient
{
    private readonly ConcurrentDictionary<string, FunctionsDetails> _functionDetails = new();
    private readonly ConcurrentDictionary<string, IAsyncSubscription> _functionDetailSubscriptions = new();
    private readonly ConcurrentDictionary<string, int> _functionDetailSubscriptionCounter = new();

    internal ConcurrentDictionary<string, FunctionsDetails> FunctionDetails { get => _functionDetails; }

    private Task ListenForFunctionUpdate(string stationName, int stationVersion, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(stationName) ||
            stationVersion <= 0)
            return Task.CompletedTask;

        try
        {
            var internalStationName = MemphisUtil.GetInternalName(stationName);
            if (_functionDetailSubscriptions.TryGetValue(internalStationName, out _))
            {
                _functionDetailSubscriptionCounter.AddOrUpdate(internalStationName, 1, (_, count) => count + 1);
                return Task.CompletedTask;
            }
            var functionUpdateSubject = $"{MemphisSubjects.FUNCTIONS_UPDATE}{internalStationName}";
            var subscription = _brokerConnection.SubscribeAsync(functionUpdateSubject, FunctionUpdateEventHandler);
            if (!_functionDetailSubscriptions.TryAdd(internalStationName, subscription))
                throw new MemphisException($"Could not add subscription for {functionUpdateSubject}.");
            _functionDetailSubscriptionCounter.AddOrUpdate(internalStationName, 1, (_, count) => count + 1);
            return Task.CompletedTask;
        }
        catch (System.Exception e)
        {
            throw new MemphisException(e.Message, e);
        }

        void FunctionUpdateEventHandler(object sender, MsgHandlerEventArgs e)
        {
            if (e is null || e.Message is null)
                return;

            var jsonData = Encoding.UTF8.GetString(e.Message.Data);
            var functionsUpdate = JsonConvert.DeserializeObject<FunctionsUpdate>(jsonData);
            if (functionsUpdate is null)
                return;

            _functionDetails.AddOrUpdate(
                e.Message.Subject,
                new FunctionsDetails { PartitionsFunctions = functionsUpdate.Functions },
                (_, _) => new FunctionsDetails { PartitionsFunctions = functionsUpdate.Functions });
        }
    }

    private async Task RemoveFunctionUpdateListenerAsync(string stationName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(stationName))
                return;

            var internalStationName = MemphisUtil.GetInternalName(stationName);
            if (!_functionDetailSubscriptionCounter.TryGetValue(internalStationName, out var count) ||
                count <= 0)
                return;

            int countAfterRemoval = count - 1;
            _functionDetailSubscriptionCounter.TryUpdate(internalStationName, countAfterRemoval, count);
            if (countAfterRemoval <= 0)
            {
                if (_functionDetailSubscriptions.TryGetValue(internalStationName, out var subscriptionToRemove))
                {
                    await subscriptionToRemove.DrainAsync();
                    _functionDetailSubscriptions.TryRemove(internalStationName, out _);
                }
                _functionDetails.TryRemove(internalStationName, out _);
            }
        }
        catch (System.Exception e)
        {
            throw new MemphisException(e.Message, e);
        }
    }
}
