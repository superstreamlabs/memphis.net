#nullable disable

namespace Memphis.Client.Producer;

public class MemphisProducerOptions
{
    /// <summary>
    /// If set, producer will be produce for this station. Only one of <see cref="StationName"/> or <see cref="StationNames"/> can be set.
    /// </summary>
    public string StationName { get; set; }
    public string ProducerName { get; set; }
    
    [Obsolete("GenerateUniqueSuffix will be stopped to be supported after November 1'st, 2023.")]
    public bool GenerateUniqueSuffix { get; set; }
    
    public int MaxAckTimeMs { get; set; } = 30_000;

    /// <summary>
    /// If set, producer will be produce for all stations in the list. Only one of <see cref="StationName"/> or <see cref="StationNames"/> can be set.
    /// </summary>
    public IEnumerable<string> StationNames { get; set; } = Enumerable.Empty<string>();


    internal void EnsureOptionIsValid()
    {
        if (!string.IsNullOrWhiteSpace(StationName) &&
            StationNames is not null &&
            StationNames.Any())
            throw new MemphisException($"Invalid configuration. Only one of {nameof(StationName)} or {nameof(StationNames)} can be set.");
    }

}