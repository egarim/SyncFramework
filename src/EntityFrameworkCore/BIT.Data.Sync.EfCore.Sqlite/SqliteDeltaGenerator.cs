
using BIT.EfCore.Sync;
using Microsoft.EntityFrameworkCore.Sqlite.Storage.Internal;
using Microsoft.EntityFrameworkCore.Sqlite.Update.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using System;
namespace BIT.Data.Sync.EfCore.SQLite
{
    public class SqliteDeltaGenerator : DeltaGeneratorBase
    {

        public SqliteDeltaGenerator()
        {

        }

        public override IUpdateSqlGenerator CreateInstance(IServiceProvider serviceProvider)
        {
            TypeMappingSourceDependencies TypeMappingSourceDependencies = serviceProvider.GetService(typeof(TypeMappingSourceDependencies)) as TypeMappingSourceDependencies;
            RelationalTypeMappingSourceDependencies RelationalTypeMappingSourceDependencies = serviceProvider.GetService(typeof(RelationalTypeMappingSourceDependencies)) as RelationalTypeMappingSourceDependencies;



            SqliteTypeMappingSource typeMappingSource = new SqliteTypeMappingSource(TypeMappingSourceDependencies, RelationalTypeMappingSourceDependencies);



            SqliteUpdateSqlGenerator sqliteUpdateSqlGenerator = new SqliteUpdateSqlGenerator(
                new UpdateSqlGeneratorDependencies(
                    new SqliteSqlGenerationHelper(
                        new RelationalSqlGenerationHelperDependencies()),
                  typeMappingSource));
           



            return sqliteUpdateSqlGenerator;
        }
    }
}
