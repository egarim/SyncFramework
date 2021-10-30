using System;
using System.Collections.Generic;

namespace BIT.Data.Sync.EfCore.Data
{
    [Serializable]
    public class ModificationCommandData
    {
        public string SourceDbEngine { get; set; }
        public int AffectedRows { get; set; }
        public ModificationCommandData(List<Parameters> parameters, List<EfSqlCommandData> sqlCommandTexts, int AffectedRows,string sourceDbEngine)
        {

            this.parameters = parameters;
            this.SqlCommandTexts = sqlCommandTexts;
            this.AffectedRows = AffectedRows;
            this.SourceDbEngine = sourceDbEngine;

        }
        public ModificationCommandData(IEnumerable<Parameters> parameters, IEnumerable<EfSqlCommandData> sqlCommandTexts, int AffectedRows, string sourceDbEngine)
        {

            this.parameters = new List<Parameters>(parameters);
            this.SqlCommandTexts = new List<EfSqlCommandData>(sqlCommandTexts);
            this.AffectedRows = AffectedRows;
            this.SourceDbEngine = sourceDbEngine;
        }
        public ModificationCommandData()
        {
            parameters = new List<Parameters>();
            SqlCommandTexts = new List<EfSqlCommandData>();
        }
        public List<Parameters> parameters { get; set; }
        public List<EfSqlCommandData> SqlCommandTexts { get; set; }

    }
}
