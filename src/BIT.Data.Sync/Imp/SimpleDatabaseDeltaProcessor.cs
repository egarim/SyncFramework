using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using BIT.Data.Sync.EventArgs;

namespace BIT.Data.Sync.Imp
{
    public class SimpleDatabaseDeltaProcessor :DeltaProcessorBase 
    {
        
        List<SimpleDatabaseRecord> _CurrentData;
        public SimpleDatabaseDeltaProcessor(List<SimpleDatabaseRecord> CurrentData,ISequenceService sequenceService) : base(sequenceService)
        {
            _CurrentData= CurrentData;
        }
        public override Task ProcessDeltasAsync(IEnumerable<IDelta> deltas, CancellationToken cancellationToken)
        {
           
            cancellationToken.ThrowIfCancellationRequested();

            foreach (IDelta delta in deltas)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var processingDeltaEventArgs = new ProcessingDeltaEventArgs(delta);

                // Raise the event
                OnProcessingDelta(processingDeltaEventArgs);

                // Check if the event handling should be canceled
                if (processingDeltaEventArgs.Handled)
                {
                    return Task.CompletedTask;
                }
                var Modification= this.GetDeltaOperations<SimpleDatabaseModification>(delta);
                switch (Modification.Operation)
                {
                    case OperationType.Add:
                        this._CurrentData.Add(Modification.Record);
                        break;
                    case OperationType.Delete:
                        var ObjectToDelete=  this._CurrentData.FirstOrDefault(x=>x.Key==Modification.Record.Key);
                        this._CurrentData.Remove(ObjectToDelete);
                        break;
                    case OperationType.Update:
                        var ObjectToUpdate = this._CurrentData.FirstOrDefault(x => x.Key == Modification.Record.Key);
                        var Index= this._CurrentData.IndexOf(ObjectToUpdate);
                        this._CurrentData[Index] = Modification.Record;
                        break;
                }
                OnProcessedDelta(new ProcessedDeltaEventArgs(delta));
                
            }
            return Task.CompletedTask;
            
        }
    }
}
