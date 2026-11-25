using BIT.Data.Sync.EfCore.Data;
using System.Collections.Generic;
using System.Linq;

namespace BIT.Data.Sync.EfCore
{
    /// <summary>
    /// Reads the provider-neutral <see cref="RowChange"/>s recorded in a delta by <see cref="SyncFrameworkBatchExecutor"/>.
    /// </summary>
    public static class DeltaRowChanges
    {
        /// <summary>
        /// Returns the row changes of <paramref name="delta"/> in the order they were saved; empty when the delta
        /// was recorded with <see cref="IEnableRowChangeCapture.EnableRowChangeCapture"/> off.
        /// </summary>
        /// <exception cref="RowChangeSerializationException">A row change in the delta is malformed.</exception>
        public static List<RowChange> GetRowChanges(IDelta delta)
        {
            var data = SerializationHelper.DecompressCore(delta.Operation);
            var modifications = SerializationHelper.DeserializeCore<List<ModificationCommandDataWithRowChange>>(data);
            return modifications.Select(m => m.GetRowChange()).Where(r => r != null).ToList();
        }
    }
}
