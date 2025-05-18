using BIT.Data.Sync;
using BIT.Data.Sync.Imp;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.Xpo;
using System;
using System.Threading.Tasks;

namespace SynFrameworkStudio.Module.BusinessObjects
{
    public class XpoSequenceService : SequenceServiceBase, ISequenceService
    {
        private readonly IObjectSpaceProvider _objectSpaceProvider;

        public XpoSequenceService(IObjectSpaceProvider objectSpaceProvider) : base(new YearSequencePrefixStrategy())
        {
            _objectSpaceProvider = objectSpaceProvider;
        }
        protected XpoSequenceService(ISequencePrefixStrategy sequencePrefixStrategy) : base(sequencePrefixStrategy)
        {
        }

        public override Task<string> GetFirstIndexValue()
        {
            return Task.FromResult("-1");
        }

        public override async Task<string> GenerateNextSequenceAsync(string prefix)
        {
            IObjectSpace ObjectSpace = _objectSpaceProvider.CreateObjectSpace();
            // Using PessimisticLocking for thread safety


            // Get or create the sequence object
            var sequence = ObjectSpace.FindObject<XpoSequence>(
                CriteriaOperator.Parse("SequenceName = ?", prefix ?? string.Empty));

            if (sequence == null)
            {
                sequence = ObjectSpace.CreateObject<XpoSequence>();
                sequence.SequenceName = prefix ?? string.Empty;
                sequence.CurrentValue = 0;
            }

            // Increment the sequence value
            sequence.CurrentValue++;
            sequence.LastUpdated = DateTime.UtcNow;

            // Save changes to persist the new sequence value
            ObjectSpace.CommitChanges();

            // Format the result based on the prefix
            if (string.IsNullOrEmpty(prefix))
                return sequence.CurrentValue.ToString("D10");
            else
                return $"{prefix}{sequence.CurrentValue:D10}";
        }
    }
}