using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace BIT.EfCore.Sync
{
    public class EFSyncStatus
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key, Column(Order = 0)]
        public Guid Id { get; set; }
        [Required]
        public string Identity { get; set; }
        public int LastTransactionLogProcessed { get; set; }
        public Guid LastProcessedDelta { get; set; }
        public Guid LastPushedDelta { get; set; }
        public EFSyncStatus()
        {

        }
    }
}
