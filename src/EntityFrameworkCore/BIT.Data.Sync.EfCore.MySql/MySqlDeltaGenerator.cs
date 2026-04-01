using BIT.EfCore.Sync;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using MySql.EntityFrameworkCore.Infrastructure.Internal;
using System;
using System.Reflection;

namespace BIT.Data.Sync.EfCore.MySql
{
    public class MySqlDeltaGenerator : DeltaGeneratorBase
    {
        public MySqlDeltaGenerator() { }

        public override IUpdateSqlGenerator CreateInstance(IServiceProvider serviceProvider)
        {
            var mysqlAsm = typeof(IMySQLOptions).Assembly;

            // Get internal types via reflection
            var helperType = mysqlAsm.GetType("MySql.EntityFrameworkCore.Storage.Internal.MySQLSqlGenerationHelper");
            var tmsType    = mysqlAsm.GetType("MySql.EntityFrameworkCore.Storage.Internal.MySQLTypeMappingSource");
            var genType    = mysqlAsm.GetType("MySql.EntityFrameworkCore.MySQLUpdateSqlGenerator");

            // Get IMySQLOptions from the service provider
            var mySqlOptions = serviceProvider.GetService(typeof(IMySQLOptions));

            // Create MySQLSqlGenerationHelper(RelationalSqlGenerationHelperDependencies, IMySQLOptions)
            var helperDeps = new RelationalSqlGenerationHelperDependencies();
            var sqlGenerationHelper = (ISqlGenerationHelper)Activator.CreateInstance(
                helperType,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                null,
                new object[] { helperDeps, mySqlOptions },
                null);

            // Get TypeMapping dependencies from service provider
            var typeMappingDeps         = (TypeMappingSourceDependencies)serviceProvider.GetService(typeof(TypeMappingSourceDependencies));
            var relTypeMappingDeps      = (RelationalTypeMappingSourceDependencies)serviceProvider.GetService(typeof(RelationalTypeMappingSourceDependencies));

            // Create MySQLTypeMappingSource(TypeMappingSourceDependencies, RelationalTypeMappingSourceDependencies, IMySQLOptions)
            var typeMappingSource = (IRelationalTypeMappingSource)Activator.CreateInstance(
                tmsType,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                null,
                new object[] { typeMappingDeps, relTypeMappingDeps, mySqlOptions },
                null);

            // Create MySQLUpdateSqlGenerator(UpdateSqlGeneratorDependencies)
            var updateDeps = new UpdateSqlGeneratorDependencies(sqlGenerationHelper, typeMappingSource);
            var generator = (IUpdateSqlGenerator)Activator.CreateInstance(
                genType,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                null,
                new object[] { updateDeps },
                null);

            return generator;
        }
    }
}
