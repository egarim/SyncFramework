using System;

namespace BIT.Data.Sync.EfCore.Data
{
    [Serializable]
    public class Parameters
    {

        public Parameters()
        {

        }
        public string Name { get; set; }
        public object Value { get; set; }


    }
}
