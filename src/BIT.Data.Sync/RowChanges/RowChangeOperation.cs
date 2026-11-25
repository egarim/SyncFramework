namespace BIT.Data.Sync
{
    /// <summary>
    /// The kind of change applied to a single row, independent of any database provider.
    /// </summary>
    public enum RowChangeOperation
    {
        /// <summary>A new row was inserted.</summary>
        Insert = 0,
        /// <summary>An existing row was updated.</summary>
        Update = 1,
        /// <summary>An existing row was deleted.</summary>
        Delete = 2
    }
}
