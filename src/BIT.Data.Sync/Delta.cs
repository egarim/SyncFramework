using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace BIT.Data.Sync
{

    /// <summary>
    /// An implementation of the IDelta interface, this class is primarily used for serialization and transportation purposes.
    /// </summary>
    /// <remarks>
    /// Every public constructor goes through the same private constructor, so each new delta has a DeltaId,
    /// a Date and an Epoch and can be saved by a delta store and pushed by a sync client.
    /// </remarks>
    [DataContract()]
    [KnownType(typeof(Delta))]
    public class Delta : IDelta
    {
        static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Delta() : this(null, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Constructor that initializes the Identity and Operation properties.
        /// </summary>
        /// <param name="identity">The identity of the delta.</param>
        /// <param name="operation">The operation of the delta.</param>
        public Delta(string identity, byte[] operation) : this(null, null, null, identity, null, operation)
        {
        }

        /// <summary>
        /// Constructor that initializes the properties based on an existing IDelta instance.
        /// </summary>
        /// <param name="Delta">The existing IDelta instance.</param>
        public Delta(IDelta Delta) : this(Delta.DeltaId, Delta.Date, Delta.Epoch, Delta.Identity, Delta.Index, Delta.Operation)
        {
        }

        /// <summary>
        /// Constructor that initializes the Identity, Index, and Operation properties.
        /// </summary>
        /// <param name="identity">The identity of the delta.</param>
        /// <param name="index">The index of the delta.</param>
        /// <param name="operation">The operation of the delta.</param>
        public Delta(string identity, string index, byte[] operation) : this(null, null, null, identity, index, operation)
        {
        }

        /// <summary>
        /// The one place where the IDelta fields are set. A value that is not given gets the shared default:
        /// a new DeltaId, the current UTC time as Date and the milliseconds from 1970-01-01 UTC to Date as Epoch.
        /// The default Date is cut to whole milliseconds, the precision Date keeps when the delta is serialized.
        /// </summary>
        private Delta(string deltaId, DateTime? date, double? epoch, string identity, string index, byte[] operation)
        {
            DateTime actualDate = date ?? CurrentUtcMilliseconds();
            DeltaId = deltaId ?? Guid.NewGuid().ToString();
            Date = actualDate;
            Epoch = epoch ?? actualDate.ToUniversalTime().Subtract(UnixEpoch).TotalMilliseconds;
            Identity = identity;
            Index = index;
            Operation = operation;
        }

        static DateTime CurrentUtcMilliseconds()
        {
            DateTime now = DateTime.UtcNow;
            return new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
        }

        /// <summary>
        /// The date of the delta.
        /// </summary>
        [DataMember]
        public virtual DateTime Date { get; set; }

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
