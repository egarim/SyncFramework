using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BIT.Data.Sync.EfCore.Data
{
    public class EfDelta : Delta, IDelta
    {
        public EfDelta()
        {

        }

        public EfDelta(string identity, byte[] operation) : base(identity, operation)
        {

        }

        public EfDelta(IDelta Delta) : base(Delta)
        {
            Date = ((Delta)Delta).Date;
        }

        public EfDelta(string identity, string index, byte[] operation) : base(identity, index, operation)
        {

        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key, Column(Order = 0)]
        public Guid Oid { get; set; }

     
    }

}
