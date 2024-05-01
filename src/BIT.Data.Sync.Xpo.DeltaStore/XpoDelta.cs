using DevExpress.Xpo;
using DevExpress.Xpo.Metadata;
using System;
using System.Linq;

namespace BIT.Data.Sync.Xpo.DeltaStore
{
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


        //[Key(autoGenerate: true)]
        //public Guid Oid
        //{
        //    get => oid;
        //    set => SetPropertyValue(nameof(Oid), ref oid, value);
        //}

        [Key(true)]
        public Guid Oid
        {
            get => oid;
            set => SetPropertyValue(nameof(Oid), ref oid, value);
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
