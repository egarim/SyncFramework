using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynFrameworkStudio.Module.BusinessObjects.Sync
{
    [DomainComponent]
    public class ClientNode : NonPersistentBaseObject
    {

        public string Name { get; set; }


    }
}
