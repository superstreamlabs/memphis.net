using System;
using System.IO;
using System.Text;
using Memphis.Client.Constants;
using Memphis.Client.Exception;
using Newtonsoft.Json;
using SolTechnology.Avro;

namespace Memphis.Client;

internal class MemphisSerializer
{
    public static byte[] Serialize<T>(T obj, string schemaType) where T : class
    {

        return schemaType switch
        {
            MemphisSchemaTypes.JSON or 
            MemphisSchemaTypes.GRAPH_QL => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj)),
            MemphisSchemaTypes.PROTO_BUF => SerializeProtoBuf(obj),
            MemphisSchemaTypes.AVRO => AvroConvert.Serialize(obj),
            _ => throw new MemphisException("Unsupported schema type"),
        };

        static byte[] SerializeProtoBuf<TData>(TData obj) where TData : class
        {
            using var stream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(stream, obj);
            return stream.ToArray();
        }
    }
}
