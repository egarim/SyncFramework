using System;
using System.Linq;
using System.Threading.Tasks;

namespace BIT.Data.Sync
{
    public abstract class SequenceServiceBase : ISequenceService
    {
        protected ISequencePrefixStrategy sequencePrefixStrategy;
        public SequenceServiceBase(ISequencePrefixStrategy sequencePrefixStrategy)
        {
            this.sequencePrefixStrategy = sequencePrefixStrategy;
        }
        public abstract Task<string> GenerateNextSequenceAsync(string Prefix);


        public virtual async Task<string> GenerateNextSequenceAsync()
        {
            return await GenerateNextSequenceAsync(sequencePrefixStrategy.GetDefaultPrefix());
        }

    }
}