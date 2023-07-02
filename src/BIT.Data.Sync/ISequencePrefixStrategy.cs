using System;
using System.Linq;

namespace BIT.Data.Sync
{
    public interface ISequencePrefixStrategy
    {
        string GetDefaultPrefix();
    }
}