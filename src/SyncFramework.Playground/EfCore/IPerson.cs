using System;

namespace SyncFramework.Playground.EfCore
{
    public interface IPerson
    {
        string FirstName { get; set; }
        Guid Id { get; set; }
        string LastName { get; set; }
        ICollection<PhoneNumber> PhoneNumbers { get; }
    }
}