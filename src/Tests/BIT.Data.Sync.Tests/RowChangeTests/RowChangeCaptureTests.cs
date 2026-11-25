using BIT.Data.Sync.EfCore;
using BIT.Data.Sync.EfCore.Data;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIT.Data.Sync.Tests.RowChangeTests
{
    /// <summary>
    /// Capture of provider-neutral row changes by SaveChanges (T-8), on SQLite in-memory databases only.
    /// </summary>
    public class RowChangeCaptureTests
    {
        private SqliteMemoryClient client;

        [SetUp]
        public void SetUp() => client = SqliteMemoryClient.Create();

        [TearDown]
        public void TearDown() => client.Dispose();

        private IDelta LastDelta() => client.DeltaStore.GetDeltasAsync(string.Empty, default).Result.Last();

        private int DeltaCount() => client.DeltaStore.GetDeltasAsync(string.Empty, default).Result.Count();

        private static string Payload(IDelta delta) => Encoding.UTF8.GetString(SerializationHelper.DecompressCore(delta.Operation));

        private string BlogsTableName => client.Context.Model.FindEntityType(typeof(Blog)).GetTableName();

        [Test]
        public void WithTheSettingOffThePayloadEqualsMains()
        {
            Assert.IsFalse(client.Context.EnableRowChangeCapture, "row change capture is off by default");

            client.Context.Add(new Blog { Name = "a" });
            client.Context.SaveChanges();

            // Captured from main (5b706c2) for the same operation: one Blog { Name = "a" } on an empty SQLite database.
            var mainPayload = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "RowChangeTests", "Fixtures", "main-sqlite-add-blog.json"));
            var payload = Payload(LastDelta());
            Assert.AreEqual(mainPayload, payload);
            StringAssert.DoesNotContain("RowChange", payload);
            StringAssert.DoesNotContain(RowChangeSerializer.FormatId, payload);
            CollectionAssert.IsEmpty(DeltaRowChanges.GetRowChanges(LastDelta()));
        }

        [Test]
        public void InsertIsCapturedWithTheGeneratedKeyAndTheColumnValues()
        {
            client.Context.EnableRowChangeCapture = true;
            var blog = new Blog { Name = "a" };
            client.Context.Add(blog);
            client.Context.SaveChanges();

            var rowChanges = DeltaRowChanges.GetRowChanges(LastDelta());
            Assert.AreEqual(1, rowChanges.Count);
            var insert = rowChanges[0];
            Assert.AreEqual(RowChangeOperation.Insert, insert.Operation);
            Assert.AreEqual("Blogs", BlogsTableName);
            Assert.AreEqual(BlogsTableName, insert.Table);
            Assert.That(blog.Id, Is.GreaterThan(0), "the database generated the key");
            CollectionAssert.AreEqual(new[] { new RowColumn("Id", blog.Id) }, insert.Keys);
            CollectionAssert.Contains(insert.Columns, new RowColumn("Name", "a"));
        }

        [Test]
        public async Task InsertIsCapturedBySaveChangesAsync()
        {
            client.Context.EnableRowChangeCapture = true;
            var blog = new Blog { Name = "async" };
            client.Context.Add(blog);
            await client.Context.SaveChangesAsync();

            var insert = DeltaRowChanges.GetRowChanges(LastDelta()).Single();
            Assert.AreEqual(RowChangeOperation.Insert, insert.Operation);
            CollectionAssert.AreEqual(new[] { new RowColumn("Id", blog.Id) }, insert.Keys);
            CollectionAssert.Contains(insert.Columns, new RowColumn("Name", "async"));
        }

        [Test]
        public void UpdateCarriesTheKeyAndTheModifiedColumnAndDeleteOnlyTheKey()
        {
            var blog = new Blog { Name = "a" };
            client.Context.Add(blog);
            client.Context.SaveChanges();
            client.Context.EnableRowChangeCapture = true;

            blog.Name = "b";
            client.Context.SaveChanges();
            var update = DeltaRowChanges.GetRowChanges(LastDelta()).Single();
            Assert.AreEqual(RowChangeOperation.Update, update.Operation);
            Assert.AreEqual(BlogsTableName, update.Table);
            CollectionAssert.AreEqual(new[] { new RowColumn("Id", blog.Id) }, update.Keys);
            CollectionAssert.AreEqual(new[] { new RowColumn("Name", "b") }, update.Columns);

            client.Context.Remove(blog);
            client.Context.SaveChanges();
            var delete = DeltaRowChanges.GetRowChanges(LastDelta()).Single();
            Assert.AreEqual(RowChangeOperation.Delete, delete.Operation);
            Assert.AreEqual(BlogsTableName, delete.Table);
            CollectionAssert.AreEqual(new[] { new RowColumn("Id", blog.Id) }, delete.Keys);
            CollectionAssert.IsEmpty(delete.Columns);
        }

        [Test]
        public void WithTheSettingOnTheSqlCommandsAreStillThereAndReadAsBefore()
        {
            client.Context.Add(new Blog { Name = "a" });
            client.Context.SaveChanges();
            var off = SerializationHelper.DeserializeCore<List<ModificationCommandData>>(SerializationHelper.DecompressCore(LastDelta().Operation)).Single();

            client.Context.EnableRowChangeCapture = true;
            client.Context.Add(new Blog { Name = "a" });
            client.Context.SaveChanges();
            // The reader of main: a list of ModificationCommandData, which ignores the extra member.
            var on = SerializationHelper.DeserializeCore<List<ModificationCommandData>>(SerializationHelper.DecompressCore(LastDelta().Operation)).Single();

            Assert.AreEqual(off.SourceDbEngine, on.SourceDbEngine);
            CollectionAssert.AreEqual(off.SqlCommandTexts.Select(c => c.DbEngine + ":" + c.Command), on.SqlCommandTexts.Select(c => c.DbEngine + ":" + c.Command));
            CollectionAssert.AreEqual(off.parameters.Select(p => p.Name), on.parameters.Select(p => p.Name));
            Assert.AreEqual("a", on.parameters.Single(p => "a".Equals(p.Value)).Value);
        }

        [Test]
        public void ACaptureThatCannotBeRecordedIsRefusedBeforeAnythingIsSaved()
        {
            client.Context.EnableRowChangeCapture = true;
            client.Context.Add(new Holiday { Day = new DateOnly(2026, 12, 25) });

            var ex = Assert.Throws<InvalidOperationException>(() => client.Context.SaveChanges());
            StringAssert.Contains("Holiday.Day", ex.Message);
            Assert.AreEqual(0, DeltaCount());
            using (var command = client.Connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM \"Holidays\";";
                Assert.AreEqual(0L, command.ExecuteScalar(), "nothing was written");
            }

            client.Context.EnableRowChangeCapture = false;
            client.Context.SaveChanges();
            Assert.AreEqual(1, DeltaCount(), "with capture off the same change saves as before");
        }
    }
}
