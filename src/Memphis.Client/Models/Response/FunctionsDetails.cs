namespace Memphis.Client;

[DataContract]
internal class FunctionsUpdate
{
    [DataMember(Name = "functions")]
    public Dictionary<int, int> Functions { get; set; }
    public FunctionsUpdate()
    {
        Functions = new();
    }
}


[DataContract]
internal class FunctionsDetails
{
    [DataMember(Name = "partitions_functions")]
    public Dictionary<int, int> PartitionsFunctions { get; set; }

    public FunctionsDetails()
    {
        PartitionsFunctions = new();
    }
}
