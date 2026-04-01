## Version 10.0.0

### MySQL Provider: Migrated from Pomelo to Official Oracle MySQL Driver
- **Replaced** `Pomelo.EntityFrameworkCore.MySql` (9.0.0, incompatible with EF Core 10) with `MySql.EntityFrameworkCore` (10.0.1, official Oracle driver) across the entire solution
- **Added** `MySqlDeltaGenerator` in `BIT.Data.Sync.EfCore.MySql` — uses reflection to instantiate internal Oracle MySQL EF Core types (`MySQLUpdateSqlGenerator`, `MySQLSqlGenerationHelper`, `MySQLTypeMappingSource`)
- **Added** `MySqlOfficial` alias in `SyncFrameworkDbContextExtensions` for the Oracle MySQL update SQL generator type
- **Added** `AddEntityFrameworkMySQL()` registration in `AddSyncFrameworkForMySql` extension (required for `IUpdateSqlGenerator` to resolve correctly in the sync DI container)
- **Updated** `EfCoreMySqlVersion` to `10.0.1` in `Directory.Build.props`
- **Fixed** `BIT.Data.Sync.EfCore.Pomelo.MySql` build error (NU1107 version conflict) by adding direct `Microsoft.EntityFrameworkCore.Relational` reference — Pomelo package is retained in solution but no longer used in tests

### Test Fixes
- **Fixed** test isolation bug in `EfDeltaStoreTests` — `SetAndGetLastProcessedDelta_Test` and `GetDeltasAsync_Test` were sharing the same in-memory database name (`nameof(SaveDeltasAsync_Test)`), causing count failures when tests ran in certain orders. Each test now uses its own database name.
- **Fixed** `LoadConnectionStrings` in `AllDatabaseTests` — changed `EnvironmentVariableTarget.User` to default (process-level) so connection strings load correctly on Linux
- **Updated** `AllDatabaseTests` to use `UseMySQL()`, `MySqlDeltaGenerator`, and `AddSyncFrameworkForMySql` (official provider API)

### Test Results (all 6 passing on .NET 10 against real databases)
- SQL Server 2022 (master node)
- SQLite (node A)
- PostgreSQL 16 (node B)
- MySQL 8.0 (node C / official Oracle driver)

## Version 2.0.1-beta

-IDeltaStore added Task<IDelta> GetDeltaAsync(string deltaId, CancellationToken cancellationToken)
-ISyncServer added Task<IDelta> GetDeltaAsync(string nodeId, string deltaId, CancellationToken cancellationToken)
-ISyncServerNode added Task<IDelta> GetDeltaAsync(string deltaId, CancellationToken cancellationToken)


## Version 2.0.0-beta
- Added `ISyncServerWithEvents` Interface.
- Added `ISyncServerNodeWithEvents` Interface.
- Added `IDeltaStoreWithEvents` Interface.
- Added `IDeltaProcessorWithEvents` Interface.

## version BIT.Data.Sync 1.0.12 

- Changed the version of the EF Core packages as shown below:
- EfCorePostgresVersion: 8.0.4
- EfCorePomeloMysqlVersion: 8.0.2
- EfCoreSqliteVersion: 8.0.5
- EfCoreSqlServerVersion: 8.0.5

## version BIT.Data.Sync 1.0.4
- Added a new extension method `AddSyncServerWithDeltaStoreNode` to `AspNetCoreServiceExtensions`.

## version BIT.Data.Sync 1.0.3
- `IDelta` now has a read and write index property: `string Index { get; set; }`.
- `DeltaStoreBase` now requires the sequence service as part of the constructor.
- `IDeltaProcessor` now has a read-only property for `SequenceService`: `ISequenceService SequenceService { get; }`.
- `IDeltaStoreExtensions.CreateDeltaCore` saves the delta creation date in UTC.

## version 7.0.3.2
- Added extension `AddSyncFrameworkForMysql`.
- Added extension `AddSyncFrameworkForNpgsql`.
- Added extension `AddSyncFrameworkForSqlite`.
- Added extension `AddSyncFrameworkForSqlServer`.
- Added `BIT.Data.Sync.AspNetCore` to host the controllers and extension for the sync server.
- Added `AddSyncServerWithMemoryNode` to add a memory node to the sync server.
- Added `AddSyncServerWithNodes` to the sync server.
- Added `SyncServer` template.

## version 7.0.3.1
## version BIT.Data.Sync 1.0.1
### ISequenceService
- Added new interface `ISequenceService` that is in charge of generating sequence numbers for delta entities.
- Added new `EfSequenceService` (ISequenceService implementation).
- Added new `MemorySequenceService` (ISequenceService implementation).
- Updated delta index comparison query to use strings instead of GUIDs.
- Breaking change: delta indexes are now strings instead of GUIDs.

## version BIT.Data.Sync 1.0.0
### BIT.Data.Sync
- Set default date to `Delta.cs` due to exception raised in some situations.
- Added new functions to `IDeltaStore`. Similarly updated `DeltaStoreBase`, `EFDeltaStore` and `MemoryDeltaStore`.
- Updated functions to allow the use of a single `DeltaStore` by multiple client nodes. 
  > It has a bug which prevents fetching deltas from `Delta` entity. At the moment, it can't be used for the same `DeltaStore` by multiple clients.
- Added `Date` in `IDelta` interface. It is added because of null exception thrown when date is not supplied.
- Added `DateTimeOffset` as known types in `SerializeHelper` class, as it fails if `DateTimeOffset` is used.
### BIT.Data.Sync.EfCore
- Fixed a bug in `SyncFrameworkBatchExecutor.cs`. The issue occurs when data is either updated or deleted.
  The `GetParameters` function adds `parameter1` as the name of the parameter instead of `@p1`, `@p2`, etc. in the generated query.
- Updated `SyncFrameworkDbContext` context.
### General
- Updated `AllDatabaseTests`.

### New Feature Added
- New sample console application added. Console app [ReadMe](#./src/SyncFramework.Console/readme.md).
- New sample `SyncServer` added.
