using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using System;

namespace SynFrameworkStudio.Module.BusinessObjects.Sync
{

    [Persistent("XpoSequences")]
   
    public class XpoSequence : BaseObject
    {
        public XpoSequence(Session session) : base(session) { }

        private string _sequenceName;
        [Size(255)]
        public string SequenceName
        {
            get => _sequenceName;
            set => SetPropertyValue(nameof(SequenceName), ref _sequenceName, value);
        }

        private long _currentValue;
        public long CurrentValue
        {
            get => _currentValue;
            set => SetPropertyValue(nameof(CurrentValue), ref _currentValue, value);
        }

        private DateTime _lastUpdated;
        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set => SetPropertyValue(nameof(LastUpdated), ref _lastUpdated, value);
        }
    }
}