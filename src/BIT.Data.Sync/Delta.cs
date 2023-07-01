
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
    public class Delta : IDelta//, ISerializable
    {
        public Delta()
        {
        }
        public static Guid GetGuid()
        {
            return GuidService.Create();
        }

    
       
        public Delta(string identity, byte[] operation)
        {

            Identity = identity;
            Operation = operation;
          
        }
        public Delta(IDelta Delta)
        {

            Identity = Delta.Identity;
            Index = Delta.Index;
            Operation = Delta.Operation;
            Epoch = Delta.Epoch;
        }
        public Delta(string identity, Guid index, byte[] operation)
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
        public virtual Guid Index { get; set; }

        [DataMember]
        public virtual byte[] Operation { get; set; }
        [DataMember]
        public virtual double Epoch { get; set; }

    }
}