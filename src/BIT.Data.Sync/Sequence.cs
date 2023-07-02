using System;
using System.Linq;

namespace BIT.Data.Sync
{
    public class Sequence : ISequence
    {

        public Sequence()
        {

        }

        public string Prefix { get; set; }
        public int LastNumber { get; set; }
    }
}