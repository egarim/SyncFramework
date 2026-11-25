namespace BIT.Data.Sync
{
    /// <summary>
    /// When implemented by a datastore it allows you to turn on or off the capture of provider-neutral
    /// <see cref="RowChange"/>s in each delta, next to the provider SQL commands.
    /// </summary>
    /// <remarks>
    /// Capture is opt-in. While it is off, the delta payload is exactly what it was before row changes existed.
    /// </remarks>
    public interface IEnableRowChangeCapture
    {
        /// <summary>
        /// True to record a <see cref="RowChange"/> for every inserted, updated or deleted row; false (the default) to record SQL commands only.
        /// </summary>
        bool EnableRowChangeCapture { get; set; }
    }
}
