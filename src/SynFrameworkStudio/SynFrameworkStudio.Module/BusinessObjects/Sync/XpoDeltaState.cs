using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using System;
using System.Linq;

namespace SynFrameworkStudio.Module.BusinessObjects.Sync
{
   
    public class XpoDeltaState : BaseObject
    {
        public XpoDeltaState(Session session) : base(session) { }

        string _Identity;
      
        public string Identity
        {
            get => _Identity;
            set => SetPropertyValue(nameof(Identity), ref _Identity, value);
        }

        string _LastProcessedDelta;
        public string LastProcessedDelta
        {
            get => _LastProcessedDelta;
            set => SetPropertyValue(nameof(LastProcessedDelta), ref _LastProcessedDelta, value);
        }

        string _LastPushedDelta;
        public string LastPushedDelta
        {
            get => _LastPushedDelta;
            set => SetPropertyValue(nameof(LastPushedDelta), ref _LastPushedDelta, value);
        }
    }
}