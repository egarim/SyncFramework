## version BIT.Data.Sync 1.0.1
### ISequenceService
    - Added new interface ISequenceService that is on charge or generating sequence numbers for delta entities
    - Added new EfSquenceService  (ISequenceService implementation)
    - Added new MemorySquenceService  (ISequenceService implementation)
    - Updated delta index comparison query to use strings instead of guids
    - Breaking change: delta indexes are now strings instead of guids

## version BIT.Data.Sync 1.0.0
### BIT.Data.Sync
- Set default date to Delta.cs due to exception raised in some situation
- Added new functions to IDeltaStore. Similarlay updated DeltaStoreBase, EFDeltaStore and MemoryDeltaStore
- Updated functions to allow the use of a single deltastore by multiple client nodes. 
  >It has a bug which prevents from fetching deltas from Delta entity. At the moment it can't be used for same deltastore by multiple clients
- Added Date in IDelat interface. It is added becasue of null exception thrown when date is not supplied.
- Added DateTimeOffset as known types in SerializeHelper class. as it fails if DateTimeOffset is used
### BIT.Data.Sync.EfCore
- Fixed a bug in `SyncFrameworkBatchExecutor.cs`. The issue occurs when data is either updated or deleted.
  The `GetParameters` function add parameter1 as the name of the parameter instead of @p1, @p2 etc. in the generated query.
- Update SyncFrameworkDbContext context.
### General
-Updated AllDatabaseTests.

### New Feature Added
- New sample console application added. Console app [ReadMe](#./src/SyncFramework.Console/readme.md) 
- New sample SyncServer added

 

