using SyncFramework.Playground.Components.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncFramework.Playground.EfCore
{
    public class Person : IPerson
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key, Column(Order = 0)]
        public Guid Id { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }



        public ICollection<PhoneNumber> PhoneNumbers { get; } = new List<PhoneNumber>();

        ICollection<IPhoneNumber> IPerson.PhoneNumbers => PhoneNumbers.Cast<IPhoneNumber>().ToList();
    }
}