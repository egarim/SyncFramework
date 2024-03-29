﻿using BIT.Data.Sync;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace BIT.EfCore.Sync
{
    public class EfSyncStatus : ISyncStatus
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key, Column(Order = 0)]
        public Guid Id { get; set; }
        [Required]
        public string Identity { get; set; }
        public int LastTransactionLogProcessed { get; set; }
        public string LastProcessedDelta { get; set; }
        public string LastPushedDelta { get; set; }
        public EfSyncStatus()
        {

        }
    }
}
