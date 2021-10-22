using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BIT.Data.Sync.EfCore.Data
{

    public class EFDelta : Delta, IDelta
    {
        public EFDelta()
        {

        }

        public EFDelta(string identity, byte[] operation) : base(identity, operation)
        {

        }

        public EFDelta(IDelta Delta) : base(Delta)
        {

        }

        public EFDelta(string identity, Guid index, byte[] operation) : base(identity, index, operation)
        {

        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key, Column(Order = 0)]
        public Guid Oid { get; set; }
    }

}
