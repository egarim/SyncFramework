using System;


namespace BIT.Data.Sync
{
    public static class GuidService
    {
        static class TimeSettings
        {

            public static int NumDateBytes { get { return 6; } }

            public static DateTime MinDateTimeValue { get; } = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);

            public static DateTime MaxDateTimeValue { get { return MinDateTimeValue.AddMilliseconds(2 ^ (8 * NumDateBytes)); } }

            public static byte[] DateTimeToBytes(DateTime timestamp)
            {
                var ms = ToUnixTimeMilliseconds(timestamp);
                var msBytes = BitConverter.GetBytes(ms);
                if (BitConverter.IsLittleEndian) Array.Reverse(msBytes);
                var result = new byte[NumDateBytes];
                var index = msBytes.GetUpperBound(0) + 1 - NumDateBytes;
                Array.Copy(msBytes, index, result, 0, NumDateBytes);
                return result;
            }

            public static DateTime BytesToDateTime(byte[] value)
            {
                // Attempt to convert the first 6 bytes.
                var msBytes = new byte[8];
                var index = 8 - NumDateBytes;
                Array.Copy(value, 0, msBytes, index, NumDateBytes);
                if (BitConverter.IsLittleEndian) Array.Reverse(msBytes);
                var ms = BitConverter.ToInt64(msBytes, 0);
                return FromUnixTimeMilliseconds(ms);
            }

            // From private DateTime.TicksPerMillisecond
            private  const long TicksPerMillisecond = 10000;

            // We purposefully are not using the FromUnixTimeMilliseconds and ToUnixTimeMilliseconds to remain compatible with .NET 4.5.1
            //(long)(timestamp.ToUniversalTime() - MinDateTimeValue).TotalMilliseconds;
            //public DateTime FromUnixTimeMilliseconds(long ms) => MinDateTimeValue.AddMilliseconds(ms);

            public static long ToUnixTimeMilliseconds(DateTime timestamp) => (timestamp.Ticks - MinDateTimeValue.Ticks) / TicksPerMillisecond;

            public static DateTime FromUnixTimeMilliseconds(long ms) => MinDateTimeValue.AddTicks(ms * TicksPerMillisecond);

        }
       
        public static Guid Create(Guid value, DateTime timestamp)
        {
            var gbytes = value.ToByteArray();
            var dbytes = TimeSettings.DateTimeToBytes(timestamp);
            Array.Copy(dbytes, 0, gbytes, 0, TimeSettings.NumDateBytes);
            SwapByteOrderForStringOrder(gbytes);
            return new Guid(gbytes);
        }

        public static DateTime GetTimestamp(Guid comb)
        {
            var gbytes = comb.ToByteArray();
            var dbytes = new byte[TimeSettings.NumDateBytes];
            SwapByteOrderForStringOrder(gbytes);
            Array.Copy(gbytes, 0, dbytes, 0, TimeSettings.NumDateBytes);
            return TimeSettings.BytesToDateTime(dbytes);
        }

       
        private static void SwapByteOrderForStringOrder(byte[] input)
        {
            Array.Reverse(input, 0, 4);             // Swap around the first 4 bytes
            if (input.Length == 4) return;
            Array.Reverse(input, 4, 2);             // Swap around the next 2 bytes
        }
        public static Guid Create()
        {
            return GuidService.Create(Guid.NewGuid(), DateTime.UtcNow);
        }

    }
}