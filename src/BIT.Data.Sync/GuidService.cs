using System;
using System.Security.Cryptography;
using System.Threading;

namespace BIT.Data.Sync
{
    public static class GuidService
    {
        public static Guid Create()
        {
            Thread.Sleep(200);
            byte[] randomBytes = new byte[10];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(randomBytes);
            }

            long timestamp = DateTime.UtcNow.Ticks / 10000L;
            byte[] timestampBytes = BitConverter.GetBytes(timestamp);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(timestampBytes);
            }

            byte[] guidBytes = new byte[16];
            Buffer.BlockCopy(timestampBytes, 2, guidBytes, 0, 6);
            Buffer.BlockCopy(randomBytes, 0, guidBytes, 6, 10);

            return new Guid(guidBytes);
        }
    }
}