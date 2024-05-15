using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync
{
    /// <summary>
    /// Defines the contract for a sequence, which provides a prefix and a last number.
    /// </summary>
    public interface ISequence
    {
        /// <summary>
        /// Gets or sets the prefix of the sequence.
        /// </summary>
        string Prefix { get; set; }

        /// <summary>
        /// Gets or sets the last number of the sequence.
        /// </summary>
        int LastNumber { get; set; }
    }
}