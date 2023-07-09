
using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace BIT.Data.Sync
{
    [DataContract()]
    [KnownType(typeof(Delta))]
    /// <summary>
    /// An implementation of the IDelta interface, this class is primary used for serialization and transportation purpose 
    /// </summary>
    public class Delta : IDelta
    {
        public Delta()
        {
        }
        

    
       
        public Delta(string identity, byte[] operation)
        {

            Identity = identity;
            Operation = operation;
          
        }
        public void SetDeltaInfo()
        {
            
        }
        public Delta(IDelta Delta)
        {

            Identity = Delta.Identity;
            Index = Delta.Index;
            Operation = Delta.Operation;
            Epoch = Delta.Epoch;
        }
        public Delta(string identity, string index, byte[] operation)
        {

            Identity = identity;
            Index = index;
            Operation = operation;
         
        }
        [DataMember]
        public virtual DateTime Date { get; set; } = DateTime.UtcNow;
        [DataMember]
        public virtual string Identity { get; set; }

        [DataMember]
        public virtual string Index { get; set; }

        [DataMember]
        public virtual byte[] Operation { get; set; }
        [DataMember]
        public virtual double Epoch { get; set; }

    }
}