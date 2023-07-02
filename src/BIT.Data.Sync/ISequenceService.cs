using System;
using System.Linq;
using System.Threading.Tasks;

namespace BIT.Data.Sync
{
    public interface ISequenceService
    {
        public Task<string> GenerateNextSequenceAsync(string Prefix);
        public Task<string> GenerateNextSequenceAsync();
    }
}