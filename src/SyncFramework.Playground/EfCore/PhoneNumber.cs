using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncFramework.Playground.EfCore
{
    public class PhoneNumber : IPhoneNumber
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key, Column(Order = 0)]
        public Guid Id { get; set; }

        public string Number { get; set; }

        public Person Person { get; set; }
    }
}