using DevExpress.Xpo;
using System;
using System.Linq;

namespace BIT.Data.Sync.Xpo.DeltaStore
{
    public class XpoSequenceService : SequenceServiceBase, ISequenceService
    {

        protected IDataLayer dataLayer;
        public XpoSequenceService(ISequencePrefixStrategy sequencePrefixStrategy, IDataLayer dataLayer) : base(sequencePrefixStrategy)
        {
            this.dataLayer = dataLayer;
        }
        protected virtual UnitOfWork GetUnitOfWork()
        {
            return new UnitOfWork(dataLayer);
        }
        public override async Task<string> GenerateNextSequenceAsync(string prefix)
        {
            var UoW = GetUnitOfWork();
            var sequence = UoW.Query<XpoSequence>().FirstOrDefault(s => s.Prefix == prefix);
            if (sequence == null)
            {
                sequence = new XpoSequence(UoW) { Prefix = prefix, LastNumber = 0 };

            }

            sequence.LastNumber++;

            await UoW.CommitChangesAsync();

            return $"{prefix}{sequence.LastNumber:D10}";
        }


    }
}
