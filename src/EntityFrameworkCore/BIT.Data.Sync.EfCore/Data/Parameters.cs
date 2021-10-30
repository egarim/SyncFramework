using System;
using System.Data;

namespace BIT.Data.Sync.EfCore.Data
{
    [Serializable]
    public class Parameters
    {
        public string CrlType { get; set; }
        public Parameters()
        {

        }
        public DbType? DbType { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }


    }
}
