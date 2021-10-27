using BIT.Data.Sync.EfCore.Data;
using Microsoft.EntityFrameworkCore.Update;
using System;
using System.Collections.Generic;

namespace BIT.EfCore.Sync
{
    public interface IModificationCommandToCommandData
    {
        IEnumerable<EfSqlCommandData> AppendDeleteOperation(ModificationCommand command);
        IEnumerable<EfSqlCommandData> AppendInsertOperation(ModificationCommand command);
        IEnumerable<EfSqlCommandData> AppendUpdateOperation(ModificationCommand command);
        Dictionary<string, IUpdateSqlGenerator> UpdateSqlGenerators { get; }
        void RegisterDeltaGenerators(IServiceProvider serviceProvider);
    }
}
