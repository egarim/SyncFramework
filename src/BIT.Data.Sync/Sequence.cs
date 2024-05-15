using System;
using System.Linq;

namespace BIT.Data.Sync
{
    /// <summary>
    /// Represents a sequence, which includes a prefix and a last number.
    /// </summary>
    public class Sequence : ISequence
    {
        /// <summary>
        /// Initializes a new instance of the Sequence class.
        /// </summary>
        public Sequence()
        {

        }

        /// <summary>
        /// Gets or sets the prefix of the sequence.
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Gets or sets the last number of the sequence.
        /// </summary>
        public int LastNumber { get; set; }
    }
}