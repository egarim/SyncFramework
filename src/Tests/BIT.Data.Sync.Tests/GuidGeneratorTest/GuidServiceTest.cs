using BIT.Data.Sync.Client;
using BIT.Data.Sync.Imp;
using BIT.Data.Sync.Tests.Infrastructure;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync.Tests.GuidGeneratorTest
{

    public static class GuidExtensions
    {
        public static long ExtractTimestamp(this Guid guid)
        {
            byte[] bytes = guid.ToByteArray();
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes, 0, 8);
            }

            long timestamp = BitConverter.ToInt64(bytes, 0);
            return timestamp;
        }

        public static int CompareToByTimestamp(this Guid guid1, Guid guid2)
        {
            long timestamp1 = guid1.ExtractTimestamp();
            long timestamp2 = guid2.ExtractTimestamp();

            return timestamp1.CompareTo(timestamp2);
        }
    }

    public class GuidServiceTest 
    {

        [SetUp()]
        public  void Setup()
        {
           
        }

    

        [Test]
        public void TestGuidsAreUnique()
        {
            var guids = new HashSet<Guid>();
            for (int i = 0; i < 10000; i++)
            {
                Guid guid = GuidService.Create();
                Assert.False(guids.Contains(guid));
                guids.Add(guid);
            }
        }

        [Test]
        public void TestGuidsAreSequential()
        {
            Guid first = GuidService.Create();
            Thread.Sleep(1);
            Guid second = GuidService.Create();
            Thread.Sleep(1);
            Guid third = GuidService.Create();

            Assert.True(first.CompareTo(second) < 0);
            Assert.True(second.CompareTo(third) < 0);
        }
        [Test]
        public void TestGuidsAreSortableAndMaintainOrder()
        {
            var guidsWithCreationTime = new List<(Guid guid, long ticks)>();
            for (int i = 0; i < 100; i++)
            {
                Thread.Sleep(1);
                var guid = GuidService.Create();
                guidsWithCreationTime.Add((guid, ExtractTicksFromGuid(guid)));
            }

            var sortedGuids = guidsWithCreationTime.OrderBy(g => g.ticks).Select(g => g.guid).ToList();

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(guidsWithCreationTime[i].guid, sortedGuids[i]);
            }
        }
        [Test]
        public void TestGuidsOrderComparison()
        {
            var guidsWithCreationTime = new List<(Guid guid, long ticks)>();
            for (int i = 0; i < 10; i++)
            {
                //Thread.Sleep(1);
                var guid = GuidService.Create();
                guidsWithCreationTime.Add((guid, ExtractTicksFromGuid(guid)));
            }

            var Position2= guidsWithCreationTime[1].guid;
            var GreaterThanPosition2= guidsWithCreationTime.Count(item=>item.guid.CompareToByTimestamp(Position2)>0);


        
        
        }
        private long ExtractTicksFromGuid(Guid guid)
        {
            var bytes = guid.ToByteArray();
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes, 0, 8);
            }

            return BitConverter.ToInt64(bytes, 0);
        }
    }
}