using System;
using System.IO;
using System.Text;
using Memphis.Client.Constants;
using Memphis.Client.Exception;
using Newtonsoft.Json;
using SolTechnology.Avro;

namespace Memphis.Client;

public class MessageSerializer
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
}