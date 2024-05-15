using System;
using System.Linq;

namespace BIT.Data.Sync
{
    /// <summary>
    /// Defines the contract for a sequence prefix strategy, which provides a method for getting the default prefix.
    /// </summary>
    public interface ISequencePrefixStrategy
    {
        /// <summary>
        /// Gets the default prefix for a sequence.
        /// </summary>
        /// <returns>The default prefix.</returns>
        string GetDefaultPrefix();
    }
}