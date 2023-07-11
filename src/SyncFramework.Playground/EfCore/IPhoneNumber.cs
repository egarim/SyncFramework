using System;

namespace SyncFramework.Playground.EfCore
{
    public interface IPhoneNumber
    {
        Guid Id { get; set; }
        string Number { get; set; }
        Person Person { get; set; }
    }
}