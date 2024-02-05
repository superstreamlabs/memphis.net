namespace Memphis.Client;


internal class FunctionsUpdate
{
    [JsonPropertyName( "functions")]
    public Dictionary<int, int> Functions { get; set; }
    public FunctionsUpdate()
    {
        Functions = new();
    }
}


internal class FunctionsDetails
{
    [JsonPropertyName( "partitions_functions")]
    public Dictionary<int, int> PartitionsFunctions { get; set; }

    public FunctionsDetails()
    {
        PartitionsFunctions = new();
    }
}
