using System;
using System.Linq;

namespace BIT.Data.Sync.Imp
{
    public class YearSequencePrefixStrategy : ISequencePrefixStrategy
    {

        public YearSequencePrefixStrategy()
        {

        }

        public string GetDefaultPrefix()
        {
            return DateTime.UtcNow.Year.ToString();
        }
    }
}