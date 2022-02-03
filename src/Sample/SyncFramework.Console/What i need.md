To implement Sync framework we need

### SyncFramework Process from object creation to Push/Pull
**What happend when we add an object to Ef and then call save function `Entityframwork.SaveChangesAsyn`.**


The SyncFramework injected `SyncFrameworkBatchExecutor` which is inherited from `Microsoft.EntityFrameworkCore.Update.Internal.BatchExecutor`. The job of 
`SyncFrameworkBatchExecutor` is to get objects that are saved in the database. It will get those objects and create sql commands based on providers (Postgres, SqlLite etc.)
 that are registered. 
It will generate modification from the saving objects. Then those modifications are converted to deltas and then the *deltas* are saved in the *DeltaStore*.

I have to use this class to prevent from delta generation base on criteria

This is how the deltas are generated from newly added/updated/deleted objects and saved to delta store.

***Delta***:
A *delta* object is consists of timestamp and sqlcommand(actual data). The sqlcommands along with parameters and sqlcommand is generated for each provided then those commands are converted to json string and json string are converted to bytes and finally compressed.

**When we call `Entityframework.PushAsync` function**

The function will load last pushed deltas from local DeltaStore and handover to HttpClient which will send deltas to the server.

**How the pull process is done using Entityframwork.PullAsync()**

The method will check the last processed delta from localstore and get the index of the last processed delta.
It then fetch deltas from the server based no the last processed delta index.
The deltas that are fetched from server are processed and saved to database.
The last step is to update the index of the last proccessed delta
## How to add network support
Implement the following interfaces. Link to [article](#https://www.jocheojeda.com/2021/10/17/syncframework-adding-network-support/) 
### ISyncFrameworkClient [Implementation](#https://github.com/egarim/SyncFramework/blob/main/src/BIT.Data.Sync/Client/SyncFrameworkHttpClient.cs)
It has HttpClient object. This will push delta to server and pull from server
```
Task<List<Delta>> FetchAsync(Guid startindex, string identity, CancellationToken cancellationToken);
Task PushAsync(IEnumerable<IDelta> Deltas, CancellationToken cancellationToken);
```

### ISyncClientNode

The SyncClientNode has the following properties
New implement client node
### ISyncClientNodeExtensions [Implementation](#https://github.com/egarim/SyncFramework/tree/main/src/BIT.Data.Sync/Client/ISyncClientNodeExtensions.cs)
This extension will have three functions that will do fetch, pull and push operations
The client node consists of the followings
![Client Node](./resources/ClientNode.png "Client Node")

### ISyncServerNode
This will handle server node which has three functions
```
public interface ISyncServerNode
{
    string NodeId { get; set; }
    Task SaveDeltasAsync(IEnumerable<IDelta> deltas, CancellationToken cancellationToken);
    Task<IEnumerable<IDelta>> GetDeltasAsync(Guid startindex, string identity, CancellationToken cancellationToken);
    Task ProcessDeltasAsync(IEnumerable<IDelta> deltas, CancellationToken cancellationToken);
}
```
The server node looks like this

![Server Node](./resources/ServerNode.png "Server Node")

### ISyncClientNodeExtensions
This is the extension class that is responsible to push/pull delta to local delta store and It will push to the attached/set http client. This could be the local delta store or It can be a server delta store

**PushAsync** is responsible to push data to local and to connected node

In case we want to push to multiple nodes, in that case we need to change the logic in the **PushAsync** function
```
cancellationToken.ThrowIfCancellationRequested();
var LastPushedDelta = await instance.DeltaStore.GetLastPushedDeltaAsync(cancellationToken).ConfigureAwait(false);
var Deltas = await instance.DeltaStore.GetDeltasAsync(LastPushedDelta,cancellationToken).ConfigureAwait(false);
if (Deltas.Any())
{
    var Max = Deltas.Max(d => d.Index);
    await instance.SyncFrameworkClient.PushAsync(Deltas, cancellationToken).ConfigureAwait(false);
    await instance.DeltaStore.SetLastPushedDeltaAsync(Max,cancellationToken).ConfigureAwait(false);
}
```

