using BIT.Data.Sync;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using System;
using System.Linq;

namespace SynFrameworkStudio.Module.BusinessObjects.Sync
{
    [DefaultClassOptions()]
    public class XpoDeltaRecord : BaseObject, IDelta
    {
        public XpoDeltaRecord(Session session) : base(session) { }

        string _DeltaId;
      
        public string DeltaId
        {
            get => _DeltaId;
            set => SetPropertyValue(nameof(DeltaId), ref _DeltaId, value);
        }

        DateTime _Date;
        public DateTime Date
        {
            get => _Date;
            set => SetPropertyValue(nameof(Date), ref _Date, value);
        }

        double _Epoch;
        public double Epoch
        {
            get => _Epoch;
            set => SetPropertyValue(nameof(Epoch), ref _Epoch, value);
        }

        string _Identity;
        public string Identity
        {
            get => _Identity;
            set => SetPropertyValue(nameof(Identity), ref _Identity, value);
        }

        string _Index;
        public string Index
        {
            get => _Index;
            set => SetPropertyValue(nameof(Index), ref _Index, value);
        }

        byte[] _Operation;
        [Size(SizeAttribute.Unlimited)]
        public byte[] Operation
        {
            get => _Operation;
            set => SetPropertyValue(nameof(Operation), ref _Operation, value);
        }
    }
}