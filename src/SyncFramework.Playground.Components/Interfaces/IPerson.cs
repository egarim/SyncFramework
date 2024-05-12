using System;

namespace SyncFramework.Playground.Components.Interfaces
{
    public interface IPerson
    {
        string FirstName { get; set; }
        Guid Id { get; set; }
        string LastName { get; set; }
        ICollection<IPhoneNumber> PhoneNumbers { get; }
    }
}