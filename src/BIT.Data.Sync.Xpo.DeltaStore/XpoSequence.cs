using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Xpo;
using DevExpress.Xpo.Metadata;
using KeyAttribute = DevExpress.Xpo.KeyAttribute;

namespace BIT.Data.Sync.Xpo.DeltaStore
{
    public class XpoSequence : DevExpress.Xpo.XPCustomObject, ISequence
    {
        public XpoSequence(Session session) : base(session)
        { }


        Guid oid;
        int lastNumber;
        string prefix;

        [Key(autoGenerate: true)]
        public Guid Oid
        {
            get => oid;
            set => SetPropertyValue(nameof(Oid), ref oid, value);
        }

        [Size(SizeAttribute.DefaultStringMappingFieldSize)]
        public string Prefix
        {
            get => prefix;
            set => SetPropertyValue(nameof(Prefix), ref prefix, value);
        }
        
        public int LastNumber
        {
            get => lastNumber;
            set => SetPropertyValue(nameof(LastNumber), ref lastNumber, value);
        }
    }
}
