using System;
using System.Linq;
using System.Threading.Tasks;

namespace BIT.Data.Sync
{
    /// <summary>
    /// Provides a base implementation for a sequence service.
    /// </summary>
    public abstract class SequenceServiceBase : ISequenceService
    {
        /// <summary>
        /// Placeholder for the sequence length formatting.
        /// </summary>
        protected string Placeholder = "D10";

        /// <summary>
        /// The sequence prefix strategy used by the sequence service.
        /// </summary>
        protected ISequencePrefixStrategy sequencePrefixStrategy;

        /// <summary>
        /// Initializes a new instance of the SequenceServiceBase class.
        /// </summary>
        /// <param name="sequencePrefixStrategy">The sequence prefix strategy to be used.</param>
        public SequenceServiceBase(ISequencePrefixStrategy sequencePrefixStrategy)
        {
            this.sequencePrefixStrategy = sequencePrefixStrategy;
        }

        /// <summary>
        /// Generates the next sequence asynchronously with the specified prefix.
        /// </summary>
        /// <param name="Prefix">The prefix of the sequence.</param>
        /// <returns>The next sequence.</returns>
        public abstract Task<string> GenerateNextSequenceAsync(string Prefix);

        /// <summary>
        /// Generates the next sequence asynchronously with the default prefix.
        /// </summary>
        /// <returns>The next sequence.</returns>
        public virtual async Task<string> GenerateNextSequenceAsync()
        {
            return await GenerateNextSequenceAsync(sequencePrefixStrategy.GetDefaultPrefix());
        }

        /// <summary>
        /// Gets the first index value.
        /// </summary>
        /// <returns>The first index value.</returns>
        public virtual Task<string> GetFirstIndexValue()
        {
            return Task.FromResult("-1");
        }
    }
}