
using BIT.EfCore.Sync;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Sqlite.Storage.Internal;
//using Microsoft.EntityFrameworkCore.Sqlite.Update.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure.Internal;
using Pomelo.EntityFrameworkCore.MySql.Internal;
using Pomelo.EntityFrameworkCore.MySql.Storage.Internal;
using Pomelo.EntityFrameworkCore.MySql.Update.Internal;
using System;
namespace BIT.Data.Sync.EfCore.Sqlite
{
    public class PomeloMySqlDeltaGenerator : DeltaGeneratorBase
    {

       

        public PomeloMySqlDeltaGenerator(MySqlServerVersion serverVersion)
        {
            this.serverVersion = serverVersion;
        }

        private static MySqlOptions GetOptions(MySqlServerVersion serverVersion)
        {
            var optionsBuilder = new DbContextOptionsBuilder();
            var extension = new MySqlOptionsExtension()
                .WithServerVersion(serverVersion);

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            var options = new MySqlOptions();
            options.Initialize(optionsBuilder.Options);
            return options;
        }
        MySqlServerVersion serverVersion;
        public override IUpdateSqlGenerator CreateInstance(IServiceProvider serviceProvider)
        {
            TypeMappingSourceDependencies TypeMappingSourceDependencies = serviceProvider.GetService(typeof(TypeMappingSourceDependencies)) as TypeMappingSourceDependencies;
            RelationalTypeMappingSourceDependencies RelationalTypeMappingSourceDependencies = serviceProvider.GetService(typeof(RelationalTypeMappingSourceDependencies)) as RelationalTypeMappingSourceDependencies;

            var optionsTypeMapping = new MySqlOptions();
            RelationalSqlGenerationHelperDependencies dependencies = new RelationalSqlGenerationHelperDependencies();

            MySqlSqlGenerationHelper sqlGenerationHelper = new MySqlSqlGenerationHelper(dependencies, optionsTypeMapping);
            MySqlTypeMappingSource typeMappingSource = new MySqlTypeMappingSource(TypeMappingSourceDependencies, RelationalTypeMappingSourceDependencies, optionsTypeMapping);
            var optionsUpdateSqlGenerator = new MySqlOptions();
            serverVersion = new MySqlServerVersion(new Version(8, 0, 31));
            optionsUpdateSqlGenerator = GetOptions(serverVersion);
            var mySqlUpdateSqlGenerator = new MySqlUpdateSqlGenerator(new UpdateSqlGeneratorDependencies(sqlGenerationHelper, typeMappingSource), optionsUpdateSqlGenerator);



            return mySqlUpdateSqlGenerator;
        }
    }
}
