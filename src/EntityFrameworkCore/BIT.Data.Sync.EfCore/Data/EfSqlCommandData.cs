using System;

namespace BIT.Data.Sync.EfCore.Data
{
    [Serializable]
    public class EfSqlCommandData
    {

        public string Command { get; set; }
        public string DbEngine { get; set; }
        public EfSqlCommandData(string command, string dbEngine)
        {
            Command = command;
            DbEngine = dbEngine;
        }
        public EfSqlCommandData()
        {
        }
    }
}
