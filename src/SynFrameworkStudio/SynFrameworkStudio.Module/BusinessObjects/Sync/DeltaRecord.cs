using BIT.Data.Sync;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using System;
using System.Linq;

namespace SynFrameworkStudio.Module.BusinessObjects.Sync
{
    [NavigationItem("Sync")]
    [DomainComponent]
    [DefaultClassOptions()]
    public class DeltaRecord : NonPersistentBaseObject, IDelta
    {
        readonly string deltaId;
        public DeltaRecord(string deltaId, DateTime date, double epoch, string identity, string index, byte[] operation)
        {
            this.deltaId = deltaId;
            Date = date;
            Epoch = epoch;
            Identity = identity;
            Index = index;
            Operation = operation;
        }
        public DeltaRecord(IDelta delta)
        {
            this.deltaId = delta.DeltaId;
            Date = delta.Date;
            Epoch = delta.Epoch;
            Identity = delta.Identity;
            Index = delta.Index;
            Operation = delta.Operation;
        }
        public string DeltaId => deltaId;

        public DateTime Date { get; set; }
        public double Epoch { get; set; }
        public string Identity { get; set; }
        public string Index { get; set; }
        public byte[] Operation { get; set; }
    }
}