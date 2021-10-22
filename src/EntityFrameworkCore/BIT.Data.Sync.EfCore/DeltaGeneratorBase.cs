using Microsoft.EntityFrameworkCore.Update;
using System;

namespace BIT.EfCore.Sync
{
    public abstract class DeltaGeneratorBase
    {
        public abstract IUpdateSqlGenerator CreateInstance(IServiceProvider serviceProvider);

    }
}
