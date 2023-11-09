namespace Memphis.Client.Models.Response;

#nullable disable

[DataContract]
internal sealed class CreateConsumerResponse
{
    [DataMember(Name = "partitions_update")]
    public PartitionsUpdate PartitionsUpdate { get; set; }

    [DataMember(Name = "error")]
    public string Error { get; set; }
    
    [DataMember(Name = "schema_update")]
    public SchemaUpdateInit SchemaUpdate { get; set; }
}
