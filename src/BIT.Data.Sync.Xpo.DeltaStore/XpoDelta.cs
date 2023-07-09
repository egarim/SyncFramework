using DevExpress.Xpo;
using DevExpress.Xpo.Metadata;
using System;
using System.Linq;

namespace BIT.Data.Sync.Xpo.DeltaStore
{
    public class XpoSyncStatus : XPObject, ISyncStatus
    {
        public XpoSyncStatus(Session session) : base(session)
        { }

        int lastTransactionLogProcessed;
        string lastPushedDelta;
        string lastProcessedDelta;
        string identity;
        Guid id;

        [Key(autoGenerate:true)]
        public Guid Id
        {
            get => id;
            private set => SetPropertyValue(nameof(Id), ref id, value);
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
    public class XpoDelta : XPCustomObject, IDelta
    {
        public XpoDelta()
        {
        }

        public XpoDelta(Session session) : base(session)
        {
        }

        public XpoDelta(Session session, XPClassInfo classInfo) : base(session, classInfo)
        {
        }


        Guid oid;
        byte[] operation;
        string index;
        string identity;
        double epoch;
        DateTime date;



        [Key(autoGenerate: true)]
        public Guid Oid
        {
            get => oid;
            private set => SetPropertyValue(nameof(Oid), ref oid, value);
        }
        public DateTime Date
        {
            get => date;
            set => SetPropertyValue(nameof(Date), ref date, value);
        }

        public double Epoch
        {
            get => epoch;
            set => SetPropertyValue(nameof(Epoch), ref epoch, value);
        }

        [Size(SizeAttribute.DefaultStringMappingFieldSize)]
        public string Identity
        {
            get => identity;
            set => SetPropertyValue(nameof(Identity), ref identity, value);
        }


        [Size(SizeAttribute.DefaultStringMappingFieldSize)]
        public string Index
        {
            get => index;
            set => SetPropertyValue(nameof(Index), ref index, value);
        }


        public byte[] Operation
        {
            get => operation;
            set => SetPropertyValue(nameof(Operation), ref operation, value);
        }
    }
}
