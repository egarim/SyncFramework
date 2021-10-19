
using System;
using System.Collections.Generic;
using System.Linq;

namespace BIT.Data.Sync
{
    //public class ReflectionService
    //{
    //    public ReflectionService(Dictionary<string, Type> DeltaStores, Dictionary<string, Type> DeltaProcessors)
    //    {
    //        _DeltaStores = DeltaStores;
    //        _DeltaProcessors = DeltaProcessors;
    //    }
    //    public Dictionary<string, Type> _DeltaProcessors { get; set; }
    //    public Dictionary<string, Type> _DeltaStores { get; set; }
    //    public IDeltaStore CreateDeltaStore(DeltaStoreSettings options)
    //    {
    //        if (options.DeltaStoreType == null)
    //            return null;

    //        KeyValuePair<string, Type> DeltaStoreType = _DeltaStores.FirstOrDefault(ds => string.Compare(ds.Key, options.Name, StringComparison.Ordinal) == 0);

    //        IDeltaStore deltaStore = Activator.CreateInstance(DeltaStoreType.Value) as IDeltaStore;
    //        return deltaStore;
    //        //return Activator.CreateInstance(DeltaStoreType.Value, new object[] { options }) as IDeltaStore;
    //    }
    //    public IDeltaProcessor CreateDeltaProcessor(DeltaStoreSettings options)
    //    {
    //        if (options.DeltaStoreType == null)
    //            return null;

    //        KeyValuePair<string, Type> DeltaStoreType = _DeltaProcessors.FirstOrDefault(ds => string.Compare(ds.Key, options.Name, StringComparison.Ordinal) == 0);

    //        return Activator.CreateInstance(DeltaStoreType.Value, new object[] { options }) as IDeltaProcessor;
    //    }
    //}

}
