using System;
using System.Linq;
using System.Runtime.Serialization;

namespace BIT.Data.Sync.Imp
{
    [DataContract()]
    public class SimpleDatabaseRecord 
    {
        [DataMember()]
        public string Text { get; set; }
        public SimpleDatabaseRecord()
        {

        }
        [DataMember()]
        public Guid Key { get; set; }
        public override string ToString()
        {
            return $"{Key} : {Text}";
        }
    }
}
