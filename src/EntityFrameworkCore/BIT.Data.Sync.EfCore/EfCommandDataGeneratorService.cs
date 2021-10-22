using BIT.Data.Sync.EfCore.Data;
using Microsoft.EntityFrameworkCore.Update;
using System;
using System.Collections.Generic;
using System.Text;
using BIT.EfCore.Sync;
using Microsoft.EntityFrameworkCore.Infrastructure;
namespace BIT.Data.Sync.EfCore
{
    public class EfCommandDataGeneratorService : IEfCommandDataGeneratorService
    {
        protected Dictionary<string, IUpdateSqlGenerator> _UpdateGenerators;
        protected IEnumerable<DeltaGeneratorBase> _deltaGenerators;
        public EfCommandDataGeneratorService(IEnumerable<DeltaGeneratorBase> deltaGenerators)
        {
            _UpdateGenerators = new Dictionary<string, IUpdateSqlGenerator>();
            _deltaGenerators = new List<DeltaGeneratorBase>(deltaGenerators);

        }
        public Dictionary<string, IUpdateSqlGenerator> UpdateGenerators => _UpdateGenerators;

        public virtual IEnumerable<EfSqlCommandData> AppendDeleteOperation(ModificationCommand command)
        {
            List<EfSqlCommandData> SqlCommands = new List<EfSqlCommandData>(UpdateGenerators.Count);
            foreach (KeyValuePair<string, IUpdateSqlGenerator> UpdateGenerator in UpdateGenerators)
            {
                StringBuilder builder = new StringBuilder();
                UpdateGenerator.Value.AppendDeleteOperation(builder, command, 0);
                SqlCommands.Add(new EfSqlCommandData(builder.ToString(), UpdateGenerator.Key));
            }
            return SqlCommands;
        }

        public virtual IEnumerable<EfSqlCommandData> AppendInsertOperation(ModificationCommand command)
        {
            List<EfSqlCommandData> SqlCommands = new List<EfSqlCommandData>(UpdateGenerators.Count);
            foreach (KeyValuePair<string, IUpdateSqlGenerator> UpdateSqlGenerator in UpdateGenerators)
            {
                StringBuilder builder = new StringBuilder();
                UpdateSqlGenerator.Value.AppendInsertOperation(builder, command, 0);
                SqlCommands.Add(new EfSqlCommandData(builder.ToString(), UpdateSqlGenerator.Key));
            }
            return SqlCommands;
        }

        public virtual IEnumerable<EfSqlCommandData> AppendUpdateOperation(ModificationCommand command)
        {
            List<EfSqlCommandData> SqlCommands = new List<EfSqlCommandData>(UpdateGenerators.Count);
            foreach (KeyValuePair<string, IUpdateSqlGenerator> UpdateSqlGenerator in UpdateGenerators)
            {
                StringBuilder builder = new StringBuilder();
                UpdateSqlGenerator.Value.AppendUpdateOperation(builder, command, 0);
                SqlCommands.Add(new EfSqlCommandData(builder.ToString(), UpdateSqlGenerator.Key));
            }
            return SqlCommands;
        }

        public virtual void RegisterDeltaGenerators(IServiceProvider serviceProvider)
        {
            var UpdaterAliasService = serviceProvider.GetService(typeof(IUpdaterAliasService)) as IUpdaterAliasService;
            RegisterCurrentUpdateSqlGenerator(serviceProvider, UpdaterAliasService);
            
            foreach (DeltaGeneratorBase deltaGeneratorBase in _deltaGenerators)
            {
                var updateSqlGenerator = deltaGeneratorBase.CreateInstance(serviceProvider);
                string key = UpdaterAliasService.GetAlias(updateSqlGenerator.GetType().FullName);
                UpdateGenerators.Add(key, updateSqlGenerator);
            }
        }

        protected virtual void RegisterCurrentUpdateSqlGenerator(IServiceProvider serviceProvider, IUpdaterAliasService updaterAliasService)
        {
            IUpdateSqlGenerator CurrentUpdater = serviceProvider.GetService(typeof(IUpdateSqlGenerator)) as IUpdateSqlGenerator;
            Type CurrentUpdaterType = CurrentUpdater.GetType();
            string fullName = CurrentUpdaterType.FullName;
            string key = updaterAliasService.GetAlias(fullName);
            UpdateGenerators.Add(key, CurrentUpdater);
        }
    }
}
