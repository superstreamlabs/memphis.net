namespace Memphis.Client.Station;

internal enum StationPartitionResolverType
{
    Producer,
    Consumer
}

internal sealed class StationPartitionResolver
{
    private int _current;
    private readonly int[] _partitions;
    private readonly int _partitionCount;

    private readonly StationPartitionResolverType _type;
    private readonly SemaphoreSlim _semaphore = new(1, 1);


    public StationPartitionResolver(int[] partitions)
    {
        _partitions = partitions;
        _partitionCount = partitions.Length;
        _current = 0;
        _type = StationPartitionResolverType.Producer;
    }

    public StationPartitionResolver(int partitionCount)
    {
        _partitionCount = partitionCount;
        _current = 0;
        _type = StationPartitionResolverType.Consumer;
        _partitions = new int[0];
    }

    public int Resolve()
    {
        try
        {
            _semaphore.Wait();

            var partition = _partitions[_current];
            _current = (_current + 1) % _partitionCount;
            return partition;
        }
        finally
        {
            _semaphore.Release();
        }
    }

}
