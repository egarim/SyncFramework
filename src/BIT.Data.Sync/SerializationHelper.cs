using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Json;
using System.Text;

namespace BIT.Data.Sync
{
    public static class SerializationHelper
    {
        public static byte[] CompressCore(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(data, 0, data.Length);
                zipStream.Close();
                return compressedStream.ToArray();
            }
        }
        public static byte[] DecompressCore(byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }

        public static byte[] SerializeCore(object Instance)
        {

            var knownTypes = new[] { typeof(DateTimeOffset) };
            DataContractJsonSerializer js = new DataContractJsonSerializer(Instance.GetType(), new DataContractJsonSerializerSettings
            {
                KnownTypes = knownTypes
            }) ;
            MemoryStream msObj = new MemoryStream();
            js.WriteObject(msObj, Instance);
            msObj.Position = 0;
            StreamReader sr = new StreamReader(msObj);
            string jsonDeltas = sr.ReadToEnd();

            byte[] bytes = Encoding.UTF8.GetBytes(jsonDeltas);
            return bytes;
           
        }
        public static T DeserializeCore<T>(byte[] Data)
        {
            string str = Encoding.UTF8.GetString(Data);
            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(str)))
            {
                var knowTypes = new Type[]{ typeof(DateTimeOffset) };

                DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(T), knowTypes);
                T Instance = (T)deserializer.ReadObject(ms);
                return Instance;

            }
        }

        public static IDelta CreateDeltaCore(string Identity, object Operations)
        {
            DateTime now = DateTime.Now;
            var delta = new Delta() { Date = now, Identity = Identity,  Operation = CompressCore(SerializeCore(Operations)) };
            delta.Epoch = now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            return delta;
        }
    }
}
