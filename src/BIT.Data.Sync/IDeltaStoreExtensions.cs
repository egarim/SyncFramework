using System;

namespace BIT.Data.Sync
{
    /// <summary>
    /// Provides extension methods for the IDeltaStore and IDeltaProcessor interfaces.
    /// </summary>
    public static class IDeltaStoreExtensions
    {
        /// <summary>
        /// Decompresses the operation of a delta and deserializes it into an object of type T.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="deltaStore">The delta store.</param>
        /// <param name="delta">The delta.</param>
        /// <returns>The deserialized object.</returns>
        public static T GetDeltaOperations<T>(this IDeltaProcessor deltaStore, IDelta delta)
        {
            var Data = deltaStore.Decompress(delta.Operation);
            return deltaStore.DeSerialize<T>(Data);
        }
        /// <summary>
        /// Decompresses the operation of a delta and deserializes it into an object of type T.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="deltaStore">The delta store.</param>
        /// <param name="delta">The delta.</param>
        /// <returns>The deserialized object.</returns>
        public static T GetDeltaOperations<T>(this IDeltaStore deltaStore, IDelta delta)
        {
            var Data = deltaStore.Decompress(delta.Operation);
            return deltaStore.DeSerialize<T>(Data);
        }
        /// <summary>
        /// Serializes an object into a byte array.
        /// </summary>
        /// <param name="deltaStore">The delta store.</param>
        /// <param name="Instance">The object to serialize.</param>
        /// <returns>The serialized object.</returns>
        public static byte[] Serialize(this IDeltaProcessor deltaStore, object Instance)
        {
            return SerializationHelper.SerializeCore(Instance);
        }
        /// <summary>
        /// Creates a new delta with the specified identity and operations.
        /// </summary>
        /// <param name="deltaStore">The delta store.</param>
        /// <param name="Identity">The identity of the delta.</param>
        /// <param name="Operations">The operations of the delta.</param>
        /// <returns>The created delta.</returns>
        public static IDelta CreateDelta(this IDeltaStore deltaStore, string Identity, object Operations)
        {
            return CreateDeltaCore(Identity, Operations);
        }
        /// <summary>
        /// Creates a new delta with the specified identity and operations.
        /// </summary>
        /// <param name="deltaProcessor">The delta processor.</param>
        /// <param name="Identity">The identity of the delta.</param>
        /// <param name="Operations">The operations of the delta.</param>
        /// <returns>The created delta.</returns>
        public static IDelta CreateDelta(this IDeltaProcessor deltaProcessor, string Identity, object Operations)
        {
            return CreateDeltaCore(Identity, Operations);
        }
        /// <summary>
        /// Creates a new delta with the specified identity and operations.
        /// </summary>
        /// <param name="Identity">The identity of the delta.</param>
        /// <param name="Operations">The operations of the delta.</param>
        /// <returns>The created delta.</returns>
        public static IDelta CreateDeltaCore(string Identity, object Operations)
        {
            DateTime now = DateTime.UtcNow;
           
            var delta = new Delta()
            {
                Date = now,
                Identity = Identity,

                Operation = SerializationHelper.CompressCore(SerializationHelper.SerializeCore(Operations)),
            
            };
            delta.Epoch = now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            return delta;
        }
        /// <summary>
        /// Creates a new operation with the specified operations.
        /// </summary>
        /// <param name="deltaStore">The delta store.</param>
        /// <param name="Operations">The operations of the delta.</param>
        /// <returns>The created operation.</returns>
        public static byte[] CreateOperation(this IDeltaProcessor deltaStore, object Operations)
        {
            return deltaStore.Compress(deltaStore.Serialize(Operations));
        }
        /// <summary>
        /// Compresses a byte array.
        /// </summary>
        /// <param name="deltaStore">The delta store.</param>
        /// <param name="data">The data to compress.</param>
        /// <returns>The compressed data.</returns>
        public static byte[] Compress(this IDeltaProcessor deltaStore, byte[] data)
        {
            return SerializationHelper.CompressCore(data);
        }
        /// <summary>
        /// Decompresses a byte array.
        /// </summary>
        /// <param name="deltaStore">The delta store.</param>
        /// <param name="data">The data to decompress.</param>
        /// <returns>The decompressed data.</returns>
        public static byte[] Decompress(this IDeltaStore deltaStore, byte[] data)
        {
            return SerializationHelper.DecompressCore(data);
        }
        /// <summary>
        /// Decompresses a byte array.
        /// </summary>
        /// <param name="deltaStore">The delta store.</param>
        /// <param name="data">The data to decompress.</param>
        /// <returns>The decompressed data.</returns>
        public static byte[] Decompress(this IDeltaProcessor deltaStore, byte[] data)
        {
            return SerializationHelper.DecompressCore(data);
        }
        /// <summary>
        /// Deserializes a byte array into an object of type T.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="deltaStore">The delta store.</param>
        /// <param name="Instance">The byte array to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        public static T DeSerialize<T>(this IDeltaStore deltaStore, byte[] Instance)
        {
            return SerializationHelper.DeserializeCore<T>(Instance);
        }
        /// <summary>
        /// Deserializes a byte array into an object of type T.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="deltaStore">The delta store.</param>
        /// <param name="Instance">The byte array to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        public static T DeSerialize<T>(this IDeltaProcessor deltaStore, byte[] Instance)
        {
            return SerializationHelper.DeserializeCore<T>(Instance);
        }


    }
}
