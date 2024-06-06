
using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace BIT.Data.Sync
{

    //TODO unify constructors

    /// <summary>
    /// An implementation of the IDelta interface, this class is primarily used for serialization and transportation purposes.
    /// </summary>
    [DataContract()]
    [KnownType(typeof(Delta))]
    public class Delta : IDelta
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Delta()
        {
            this.DeltaId = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Constructor that initializes the Identity and Operation properties.
        /// </summary>
        /// <param name="identity">The identity of the delta.</param>
        /// <param name="operation">The operation of the delta.</param>
        public Delta(string identity, byte[] operation):this()
        {
            Identity = identity;
            Operation = operation;
        }

        /// <summary>
        /// Constructor that initializes the properties based on an existing IDelta instance.
        /// </summary>
        /// <param name="Delta">The existing IDelta instance.</param>
        public Delta(IDelta Delta) : this()
        {
            Identity = Delta.Identity;
            Index = Delta.Index;
            Operation = Delta.Operation;
            Epoch = Delta.Epoch;
            DeltaId = Delta.DeltaId;
        }

        /// <summary>
        /// Constructor that initializes the Identity, Index, and Operation properties.
        /// </summary>
        /// <param name="identity">The identity of the delta.</param>
        /// <param name="index">The index of the delta.</param>
        /// <param name="operation">The operation of the delta.</param>
        public Delta(string identity, string index, byte[] operation)
        {
            Identity = identity;
            Index = index;
            Operation = operation;
        }

        /// <summary>
        /// The date of the delta.
        /// </summary>
        [DataMember]
        public virtual DateTime Date { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The identity of the delta.
        /// </summary>
        [DataMember]
        public virtual string Identity { get; set; }

        /// <summary>
        /// The index of the delta.
        /// </summary>
        [DataMember]
        public virtual string Index { get; set; }

        /// <summary>
        /// The operation of the delta.
        /// </summary>
        [DataMember]
        public virtual byte[] Operation { get; set; }

        /// <summary>
        /// The epoch of the delta.
        /// </summary>
        [DataMember]
        public virtual double Epoch { get; set; }
        /// <summary>
        /// The id of the delta
        /// </summary>
        [DataMember]
        public string DeltaId { get; set; }
    }
}