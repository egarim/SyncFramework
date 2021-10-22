using System;
using System.Collections.Generic;
using System.Text;

namespace BIT.Data.Sync.EfCore
{
    public interface IUpdaterAliasService
    {
        Dictionary<string,string> KnownUpdaters { get; }
        void Register(string UpdaterFullTypeName,string Alias);
        string GetAlias(string UpdaterFullTypeName);
    }
    public class UpdaterAliasService : IUpdaterAliasService
    {
        
        public UpdaterAliasService (Dictionary<string, string> KnownUpdaters)
        {
            this.KnownUpdaters = KnownUpdaters;
        }

        public Dictionary<string, string> KnownUpdaters { get; set; }

        public string GetAlias(string UpdaterFullTypeName)
        {
            if (KnownUpdaters[UpdaterFullTypeName] == null)
            {
                return UpdaterFullTypeName;
            }
            else
            {
               return KnownUpdaters[UpdaterFullTypeName];
            }
        }

        public void Register(string UpdaterFullTypeName, string Alias)
        {
            if(KnownUpdaters[UpdaterFullTypeName]==null)
            {
                KnownUpdaters.Add(UpdaterFullTypeName, Alias);
            }
            else
            {
                KnownUpdaters[UpdaterFullTypeName] = Alias;
            }
        }
    }
}
