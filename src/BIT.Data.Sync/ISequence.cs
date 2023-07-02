using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync
{
    public interface ISequence
    {
        public string Prefix { get; set; }
        public int LastNumber { get; set; }
    }
}