using System;

namespace SyncFramework.Playground.Components.Interfaces
{
    public interface IPhoneNumber
    {
        Guid Id { get; set; }
        string Number { get; set; }
        IPerson Person { get; set; }
    }
}