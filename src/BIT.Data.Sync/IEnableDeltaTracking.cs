namespace BIT.Data.Sync
{
    /// <summary>
    /// When implemented by a datastore it allows you to turn on or off delta tracking
    /// </summary>
    public interface IEnableDeltaTracking
    {
        /// <summary>
        /// True for enable delta tracking otherwise false
        /// </summary>
        bool EnableDeltaTracking { get; set; }
    }
}