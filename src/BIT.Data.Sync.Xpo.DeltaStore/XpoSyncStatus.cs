using DevExpress.Xpo;
using System;
using System.Linq;

namespace BIT.Data.Sync.Xpo.DeltaStore
{
    public class XpoSyncStatus : XPCustomObject, ISyncStatus
    {
        public XpoSyncStatus(Session session) : base(session)
        { }

        int lastTransactionLogProcessed;
        string lastPushedDelta;
        string lastProcessedDelta;
        string identity;
        Guid id;

        [Key(autoGenerate: true)]
        public Guid Id
        {
            get => id;
            set => SetPropertyValue(nameof(Id), ref id, value);
        }

        [Size(SizeAttribute.DefaultStringMappingFieldSize)]
        public string Identity
        {
            get => identity;
            set => SetPropertyValue(nameof(Identity), ref identity, value);
        }

        [Size(SizeAttribute.DefaultStringMappingFieldSize)]
        public string LastProcessedDelta
        {
            get => lastProcessedDelta;
            set => SetPropertyValue(nameof(LastProcessedDelta), ref lastProcessedDelta, value);
        }


        [Size(SizeAttribute.DefaultStringMappingFieldSize)]
        public string LastPushedDelta
        {
            get => lastPushedDelta;
            set => SetPropertyValue(nameof(LastPushedDelta), ref lastPushedDelta, value);
        }

        public int LastTransactionLogProcessed
        {
            get => lastTransactionLogProcessed;
            set => SetPropertyValue(nameof(LastTransactionLogProcessed), ref lastTransactionLogProcessed, value);
        }
    }
}
