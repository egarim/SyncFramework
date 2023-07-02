using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BIT.Data.Sync.EfCore.Data
{
    public class EfSequence : Sequence, ISequence
    {

        public EfSequence(ISequence Sequence)
        {

        }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key, Column(Order = 0)]
        public Guid Oid { get; set; }


        public EfSequence()
        {

        }
    }
}
