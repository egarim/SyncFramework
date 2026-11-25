using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace BIT.Data.Sync.EfCore.Data
{
    /// <summary>
    /// A <see cref="ModificationCommandData"/> that also carries the provider-neutral <see cref="Sync.RowChange"/> of the row.
    /// </summary>
    /// <remarks>
    /// Deltas hold a list of these only while <see cref="IEnableRowChangeCapture.EnableRowChangeCapture"/> is on.
    /// The row change is one more member after the existing ones, so the payload still reads as a list of
    /// <see cref="ModificationCommandData"/> and runs its SQL commands as before.
    /// </remarks>
    [Serializable]
    public class ModificationCommandDataWithRowChange : ModificationCommandData
    {
        public ModificationCommandDataWithRowChange()
        {
        }

        public ModificationCommandDataWithRowChange(IEnumerable<Parameters> parameters, IEnumerable<EfSqlCommandData> sqlCommandTexts, int AffectedRows, string sourceDbEngine, RowChange rowChange)
            : base(parameters, sqlCommandTexts, AffectedRows, sourceDbEngine)
        {
            RowChange = rowChange == null ? null : Encoding.UTF8.GetString(RowChangeSerializer.Serialize(rowChange));
        }

        /// <summary>
        /// The row change in the neutral "bit.rowchange/1" JSON format of <see cref="RowChangeSerializer"/>, or null.
        /// </summary>
        /// <remarks>Optional, so a payload recorded with capture off reads back with no row change.</remarks>
        [field: OptionalField]
        public string RowChange { get; set; }

        /// <summary>
        /// Reads <see cref="RowChange"/> back strictly, or returns null when there is none.
        /// </summary>
        /// <exception cref="RowChangeSerializationException">The row change is malformed.</exception>
        public RowChange GetRowChange() => RowChange == null ? null : RowChangeSerializer.Deserialize(Encoding.UTF8.GetBytes(RowChange));
    }
}
