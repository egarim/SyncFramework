
using BIT.EfCore.Sync;

using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Update.Internal;
using System;
namespace BIT.Data.Sync.EfCore.Npgsql
{
    public class NpgsqlDeltaGenerator : DeltaGeneratorBase
    {

        public NpgsqlDeltaGenerator()
        {

        }

        public override IUpdateSqlGenerator CreateInstance(IServiceProvider serviceProvider)
        {
            TypeMappingSourceDependencies TypeMappingSourceDependencies = serviceProvider.GetService(typeof(TypeMappingSourceDependencies)) as TypeMappingSourceDependencies;
            RelationalTypeMappingSourceDependencies RelationalTypeMappingSourceDependencies = serviceProvider.GetService(typeof(RelationalTypeMappingSourceDependencies)) as RelationalTypeMappingSourceDependencies;



           

            var typeMapper = new NpgsqlTypeMappingSource(TypeMappingSourceDependencies, RelationalTypeMappingSourceDependencies, new NpgsqlSqlGenerationHelper(
                        new RelationalSqlGenerationHelperDependencies()));

            var sqliteUpdateSqlGenerator = new NpgsqlUpdateSqlGenerator(
                        new UpdateSqlGeneratorDependencies(
                            new NpgsqlSqlGenerationHelper(
                                new RelationalSqlGenerationHelperDependencies()),
                            typeMapper));




            return sqliteUpdateSqlGenerator;
        }
    }
}
