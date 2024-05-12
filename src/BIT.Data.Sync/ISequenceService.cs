using System;
using System.Linq;
using System.Threading.Tasks;

namespace BIT.Data.Sync
{
    public interface ISequenceService
    {
        /// <summary>
        /// Generates the next sequence for a given prefix
        /// </summary>
        /// <param name="Prefix">Prefix text value</param>
        /// <returns>The next sequence formatted value</returns>
         Task<string> GenerateNextSequenceAsync(string Prefix);
        /// <summary>
        /// Generates the next sequence for a given prefix
        /// </summary>
        /// <returns>The next sequence formatted value</returns>
        Task<string> GenerateNextSequenceAsync();
        /// <summary>
        /// the index that represents the first value in the sequence, for example 20240000000007 is greater than -1 in a lexicographical comparison.
        /// </summary>
        /// <returns>The value of the first index</returns>
        Task<string> GetFirstIndexValue();
    }
}