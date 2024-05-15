using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Json;
using System.Text;

namespace BIT.Data.Sync
{
    /// <summary>
    /// Provides methods for compressing, decompressing, serializing, and deserializing data, and creating deltas.
    /// </summary>
    public static class SerializationHelper
    {
        /// <summary>
        /// Compresses a byte array.
        /// </summary>
        /// <param name="data">The data to compress.</param>
        /// <returns>The compressed data.</returns>
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
        /// <summary>
        /// Decompresses a byte array.
        /// </summary>
        /// <param name="data">The data to decompress.</param>
        /// <returns>The decompressed data.</returns>
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
        /// <summary>
        /// Serializes an object into a byte array.
        /// </summary>
        /// <param name="Instance">The object to serialize.</param>
        /// <returns>The serialized object.</returns>
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
        /// <summary>
        /// Deserializes a byte array into an object of type T.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="Data">The byte array to deserialize.</param>
        /// <returns>The deserialized object.</returns>
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
        /// <summary>
        /// Creates a new delta with the specified identity and operations.
        /// </summary>
        /// <param name="Identity">The identity of the delta.</param>
        /// <param name="Operations">The operations of the delta.</param>
        /// <returns>The created delta.</returns>
        public static IDelta CreateDeltaCore(string Identity, object Operations)
        {
            DateTime now = DateTime.Now;
            var delta = new Delta() { Date = now, Identity = Identity,  Operation = CompressCore(SerializeCore(Operations)) };
            delta.Epoch = now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            return delta;
        }
    }
}
