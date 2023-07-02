using System;

namespace BIT.Data.Sync
{
    public static class IDeltaStoreExtensions
    {
        public static T GetDeltaOperations<T>(this IDeltaProcessor deltaStore, IDelta delta)
        {
            var Data = deltaStore.Decompress(delta.Operation);
            return deltaStore.DeSerialize<T>(Data);
        }

        public static byte[] Serialize(this IDeltaProcessor deltaStore, object Instance)
        {
            return SerializationHelper.SerializeCore(Instance);
        }

        public static IDelta CreateDelta(this IDeltaStore deltaStore, string Identity, object Operations)
        {
            return CreateDeltaCore(Identity, Operations);
        }
        public static IDelta CreateDelta(this IDeltaProcessor deltaProcessor, string Identity, object Operations)
        {
            return CreateDeltaCore(Identity, Operations);
        }
      
        public static IDelta CreateDeltaCore(string Identity, object Operations)
        {
            DateTime now = DateTime.Now;
            var delta = new Delta()
            {
                Date = now,
                Identity = Identity,

                Operation = SerializationHelper.CompressCore(SerializationHelper.SerializeCore(Operations)),
                Index = ""// Delta.GetGuid() TODO fix this after index string change
            };
            delta.Epoch = now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            return delta;
        }

        public static byte[] CreateOperation(this IDeltaProcessor deltaStore, object Operations)
        {
            return deltaStore.Compress(deltaStore.Serialize(Operations));
        }
        public static byte[] Compress(this IDeltaProcessor deltaStore, byte[] data)
        {
            return SerializationHelper.CompressCore(data);
        }


        public static byte[] Decompress(this IDeltaProcessor deltaStore, byte[] data)
        {
            return SerializationHelper.DecompressCore(data);
        }


        public static T DeSerialize<T>(this IDeltaProcessor deltaStore, byte[] Instance)
        {
            return SerializationHelper.DeserializeCore<T>(Instance);
        }


    }
}
