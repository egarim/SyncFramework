using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BIT.Data.Sync.Imp
{

    public class MemorySequenceService : SequenceServiceBase
    {
        List<Sequence> sequences = new List<Sequence>();

        public MemorySequenceService(ISequencePrefixStrategy sequencePrefixStrategy) : base(sequencePrefixStrategy)
        {

        }

        public override Task<string> GenerateNextSequenceAsync(string Prefix)
        {
            var sequence = sequences.FirstOrDefault(s => s.Prefix == Prefix);
            if (sequence == null)
            {
                sequence = new Sequence { Prefix = Prefix, LastNumber = 0 };
                sequences.Add(sequence);
            }

            sequence.LastNumber++;

            string result = $"{Prefix}{sequence.LastNumber:D4}";
            Debug.WriteLine(result);
            return Task.FromResult(result);
        }

     
    }
}