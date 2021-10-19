using System;
using System.Linq;
using System.Runtime.Serialization;

namespace BIT.Data.Sync.Imp
{
    [DataContract()]
    public class SimpleDatabaseModification
    {

       
        public SimpleDatabaseModification(OperationType operation, SimpleDatabaseRecord record)
        {
            Operation = operation;
            Record = record;
        }
        [DataMember()]
        public SimpleDatabaseRecord Record { get; set; }
        [DataMember()]
        public OperationType Operation
        {
            get; set;
        }
}
}
