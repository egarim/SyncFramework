using BIT.Data.Sync;
using BIT.Data.Sync.EfCore.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BIT.EfCore.Sync
{

    public class EfSequenceService : SequenceServiceBase, ISequenceService
    {
        DeltaDbContext DeltaDbContext;

        public EfSequenceService(ISequencePrefixStrategy sequencePrefixStrategy, DeltaDbContext deltaDbContext) : base(sequencePrefixStrategy)
        {
            DeltaDbContext = deltaDbContext;
        }

        public override async Task<string> GenerateNextSequenceAsync(string prefix)
        {
            var sequence = DeltaDbContext.EfSequence.FirstOrDefault(s => s.Prefix == prefix);
            if (sequence == null)
            {
                sequence = new EfSequence { Prefix = prefix, LastNumber = 0 };
                DeltaDbContext.EfSequence.Add(sequence);
            }

            sequence.LastNumber++;

            await DeltaDbContext.SaveChangesAsync();

            return $"{prefix}{sequence.LastNumber:D10}";
        }


    }

}