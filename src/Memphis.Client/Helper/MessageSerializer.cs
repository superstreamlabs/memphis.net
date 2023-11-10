using SolTechnology.Avro;

namespace Memphis.Client;

internal class MessageSerializer
{
    /// <summary>
    /// Serialize the object to the specified schema type. The supported schema types are: json, graphql, protobuf, avro
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <param name="schemaType"></param>
    /// <returns></returns>
    /// <exception cref="MemphisException"></exception>
    public static byte[] Serialize<T>(T obj, string schemaType) where T : class
    {

        return schemaType switch
        {
            MemphisSchemaTypes.JSON or 
            MemphisSchemaTypes.GRAPH_QL => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj)),
            MemphisSchemaTypes.PROTO_BUF => SerializeProtoBuf(obj),
            MemphisSchemaTypes.AVRO => AvroConvert.Serialize(obj),
            _ => throw new MemphisException("Unsupported schema type, the supported schema types are: json, graphql, protobuf, avro"),
        };

        static byte[] SerializeProtoBuf<TData>(TData obj) where TData : class
        {
            using var stream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(stream, obj);
            return stream.ToArray();
        }
    }

    /// <summary>
    /// Deserialize the byte array to the specified schema type. The supported schema types are: json, graphql, protobuf, avro
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <param name="schemaType"></param>
    /// <returns></returns>
    public static T? Deserialize<T>(byte[] data, string schemaType) where T : class
    {
        return schemaType switch
        {
            MemphisSchemaTypes.JSON or 
            MemphisSchemaTypes.GRAPH_QL => JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(data)),
            MemphisSchemaTypes.PROTO_BUF => DeserializeProtoBuf<T>(data),
            MemphisSchemaTypes.AVRO => AvroConvert.Deserialize<T>(data),
            _ => throw new MemphisException("Unsupported schema type, the supported schema types are: json, graphql, protobuf, avro"),
        };

        static T DeserializeProtoBuf<TData>(byte[] data) where TData : class
        {
            using var stream = new MemoryStream(data);
            return ProtoBuf.Serializer.Deserialize<T>(stream);
        }
    }
}