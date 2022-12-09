using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Memphis.Client.Helper
{
    public class JsonSerDes
    {
        internal static string PrepareJsonString<T>(object objectToBeParsed)
        {
            DataContractJsonSerializer dataContractSerializer = new DataContractJsonSerializer(typeof(T));
            string json = string.Empty;
            using (var ms = new MemoryStream())
            {
                dataContractSerializer.WriteObject(ms, (T) objectToBeParsed);
                ms.Position = 0;
                StreamReader sr = new StreamReader(ms);
                json = sr.ReadToEnd();
            }

            return json;
        }

        internal static object PrepareObjectFromString<T>(string json)
        {
            DataContractJsonSerializer dataContractSerializer = new DataContractJsonSerializer(typeof(T));
            using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var deSerializedUser = dataContractSerializer.ReadObject(memoryStream);
                return deSerializedUser;
            }
        }
    }
}