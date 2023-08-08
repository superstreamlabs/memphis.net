using System.Runtime.Serialization;
#nullable disable
namespace Memphis.Client.Models.Response;

[DataContract]
internal sealed class PartitionsUpdate
{
    [DataMember(Name = "partitions_list")]
    public int[] PartitionsList { get; set; }
}

[DataContract]
internal sealed class CreateProducerResponse
{
    [DataMember(Name = "schema_update")]
    public ProducerSchemaUpdateInit SchemaUpdate { get; set; }

    [DataMember(Name = "error")]
    public string Error { get; set; }

    [DataMember(Name = "send_notification")]
    public bool SendNotification { get; set; }
    
    [DataMember(Name = "schemaverse_to_dls")]
    public bool SchemaVerseToDls { get; set; }

    [DataMember(Name = "partitions_update")]
    public PartitionsUpdate PartitionsUpdate { get; set; }
}