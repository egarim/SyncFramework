using BIT.Data.Sync.EfCore.Data;
using Microsoft.EntityFrameworkCore.Update;
using System;
using System.Collections.Generic;
using System.Text;
using BIT.EfCore.Sync;
using Microsoft.EntityFrameworkCore.Infrastructure;
namespace BIT.Data.Sync.EfCore
{
    public class ModificationCommandToCommandDataService : IModificationCommandToCommandDataService
    {
        protected Dictionary<string, IUpdateSqlGenerator> _UpdateSqlGenerators;
        protected IEnumerable<DeltaGeneratorBase> _deltaGenerators;
        public ModificationCommandToCommandDataService(IEnumerable<DeltaGeneratorBase> deltaGenerators)
        {
            _UpdateSqlGenerators = new Dictionary<string, IUpdateSqlGenerator>();
            _deltaGenerators = new List<DeltaGeneratorBase>(deltaGenerators);

        }
        public Dictionary<string, IUpdateSqlGenerator> UpdateSqlGenerators => _UpdateSqlGenerators;

        public virtual IEnumerable<EfSqlCommandData> AppendDeleteOperation(ModificationCommand command)
        {
            List<EfSqlCommandData> SqlCommands = new List<EfSqlCommandData>(UpdateSqlGenerators.Count);
            foreach (KeyValuePair<string, IUpdateSqlGenerator> UpdateGenerator in UpdateSqlGenerators)
            {
                StringBuilder builder = new StringBuilder();
                UpdateGenerator.Value.AppendDeleteOperation(builder, command, 0);
                SqlCommands.Add(new EfSqlCommandData(builder.ToString(), UpdateGenerator.Key));
            }
            return SqlCommands;
        }

        public virtual IEnumerable<EfSqlCommandData> AppendInsertOperation(ModificationCommand command)
        {
            List<EfSqlCommandData> SqlCommands = new List<EfSqlCommandData>(UpdateSqlGenerators.Count);
            foreach (KeyValuePair<string, IUpdateSqlGenerator> UpdateSqlGenerator in UpdateSqlGenerators)
            {
                StringBuilder builder = new StringBuilder();
                UpdateSqlGenerator.Value.AppendInsertOperation(builder, command, 0);
                SqlCommands.Add(new EfSqlCommandData(builder.ToString(), UpdateSqlGenerator.Key));
            }
            return SqlCommands;
        }

        public virtual IEnumerable<EfSqlCommandData> AppendUpdateOperation(ModificationCommand command)
        {
            List<EfSqlCommandData> SqlCommands = new List<EfSqlCommandData>(UpdateSqlGenerators.Count);
            foreach (KeyValuePair<string, IUpdateSqlGenerator> UpdateSqlGenerator in UpdateSqlGenerators)
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
                
                if (UpdateSqlGenerators.ContainsKey(key))
                {
                    UpdateSqlGenerators[key] = updateSqlGenerator;
                }
                else
                {
                    UpdateSqlGenerators.Add(key, updateSqlGenerator);
                }
            }
        }

        protected virtual void RegisterCurrentUpdateSqlGenerator(IServiceProvider serviceProvider, IUpdaterAliasService updaterAliasService)
        {
            IUpdateSqlGenerator CurrentUpdateSqlGenerator = serviceProvider.GetService(typeof(IUpdateSqlGenerator)) as IUpdateSqlGenerator;
            string key = updaterAliasService.GetAlias(CurrentUpdateSqlGenerator.GetType().FullName);
            if(UpdateSqlGenerators.ContainsKey(key))
            {
                UpdateSqlGenerators[key] = CurrentUpdateSqlGenerator;
            }
            else
            {
                UpdateSqlGenerators.Add(key, CurrentUpdateSqlGenerator);
            }
            
        }
    }
}
