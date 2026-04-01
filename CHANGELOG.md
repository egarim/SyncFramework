# Changelog

All notable changes to SyncFramework are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [10.0.0] — 2026-04-01

### Added
- **SyncFramework.Dashboard** — new Blazor Server admin UI (`src/Tools/SyncFramework.Dashboard`)
  - Nodes page: lists all active nodes with delta count and store type
  - Add Node page: spin up new nodes dynamically (Memory or SQLite store)
  - Replay page: fetch all deltas from a node and replay them into a SQLite database for inspection
  - Sync API endpoint (`/sync/*`) via `SyncControllerBase` — fully functional server included
  - Bootstrap 5 + Bootstrap Icons via CDN, no npm/build step required
- **`MySqlDeltaGenerator`** in `BIT.Data.Sync.EfCore.MySql` — delta generator for the official Oracle MySQL EF Core provider, using reflection to access internal types (`MySQLUpdateSqlGenerator`, `MySQLSqlGenerationHelper`, `MySQLTypeMappingSource`)
- **`MySqlOfficial`** updater alias in `SyncFrameworkDbContextExtensions` for `MySQLUpdateSqlGenerator`

### Changed
- **MySQL provider migrated from Pomelo to official Oracle driver**
  - `Pomelo.EntityFrameworkCore.MySql` 9.0.0 was incompatible with EF Core 10 at runtime (`MissingMethodException` on `AbstractionsStrings.ArgumentIsEmpty`)
  - Replaced with `MySql.EntityFrameworkCore` 10.0.1 (official Oracle driver, fully EF Core 10 compatible)
  - `EfCoreMySqlVersion` updated to `10.0.1` in `Directory.Build.props`
  - `AddEntityFrameworkMySQL()` added to `AddSyncFrameworkForMySql` extension (required for `IUpdateSqlGenerator` DI resolution)
  - Tests updated to use `UseMySQL()`, `MySqlDeltaGenerator`, and `AddSyncFrameworkForMySql`
- **Target framework** upgraded to `.NET 10.0`
- **EF Core packages** upgraded to `10.0.0` / `10.0.1`

### Fixed
- **Test isolation bug** in `EfDeltaStoreTests` — `SetAndGetLastProcessedDelta_Test` and `GetDeltasAsync_Test` shared the same in-memory database name (`nameof(SaveDeltasAsync_Test)`), causing intermittent count failures. Each test now uses its own unique database name.
- **Linux environment variable loading** in `AllDatabaseTests.LoadConnectionStrings` — `EnvironmentVariableTarget.User` is Windows-only; changed to default (process-level) so tests work correctly on Linux
- **Build error NU1107** in `BIT.Data.Sync.EfCore.Pomelo.MySql` — added direct `Microsoft.EntityFrameworkCore.Relational` reference to resolve version conflict (Pomelo package retained in solution for reference)

### Tests — All 6 passing on .NET 10 against real databases
| Test | Database | Result |
|---|---|---|
| `SyncFramework_CrossDatabaseSync_SuccessfullyExchangesData` | SQL Server + SQLite + PostgreSQL + MySQL | ✅ |
| `DeltasMarkedAsSentAfterPush` | SQL Server | ✅ |
| `SaveDeltasAsync_Test` | In-memory | ✅ |
| `SetAndGetLastProcessedDelta_Test` | In-memory | ✅ |
| `GetDeltasAsync_Test` | In-memory | ✅ |
| `PurgeDeltasAsync_Test` | In-memory | ✅ |

---

## [2.0.1-beta]

### Added
- `IDeltaStore.GetDeltaAsync(string deltaId, CancellationToken)` — fetch a single delta by ID
- `ISyncServer.GetDeltaAsync(string nodeId, string deltaId, CancellationToken)`
- `ISyncServerNode.GetDeltaAsync(string deltaId, CancellationToken)`

---

## [2.0.0-beta]

### Added
- `ISyncServerWithEvents` interface
- `ISyncServerNodeWithEvents` interface
- `IDeltaStoreWithEvents` interface
- `IDeltaProcessorWithEvents` interface

---

## [1.0.12]

### Changed
- EF Core package versions updated:
  - `EfCorePostgresVersion` → 8.0.4
  - `EfCorePomeloMysqlVersion` → 8.0.2
  - `EfCoreSqliteVersion` → 8.0.5
  - `EfCoreSqlServerVersion` → 8.0.5

---

## [1.0.4]

### Added
- `AddSyncServerWithDeltaStoreNode` extension method in `AspNetCoreServiceExtensions`

---

## [1.0.3]

### Changed
- `IDelta.Index` is now a read/write `string` property
- `DeltaStoreBase` constructor now requires `ISequenceService`
- `IDeltaProcessor` exposes `ISequenceService SequenceService { get; }`

### Fixed
- `IDeltaStoreExtensions.CreateDeltaCore` now saves delta creation date in UTC

---

## [7.0.3.2]

### Added
- `AddSyncFrameworkForMysql`, `AddSyncFrameworkForNpgsql`, `AddSyncFrameworkForSqlite`, `AddSyncFrameworkForSqlServer` extension methods
- `BIT.Data.Sync.AspNetCore` package — hosts controllers and sync server extensions
- `AddSyncServerWithMemoryNode` and `AddSyncServerWithNodes` to `AspNetCoreServiceExtensions`
- `SyncServer` sample project

---

## [1.0.1]

### Added
- `ISequenceService` — interface for delta sequence number generation
- `EfSequenceService` and `MemorySequenceService` implementations

### Changed
- Delta index comparison now uses strings instead of GUIDs

### Breaking Changes
- Delta indexes changed from `Guid` to `string`

---

## [1.0.0]

### Added
- `Date` property in `IDelta` (prevents null exceptions when date not supplied)
- `DateTimeOffset` registered as known type in `SerializeHelper`
- New functions in `IDeltaStore` to support multiple client nodes on a single store
- Sample console application (`src/SyncFramework.Console`)
- Sample `SyncServer` project

### Fixed
- `SyncFrameworkBatchExecutor`: parameter naming bug — was generating `parameter1` instead of `@p1`, `@p2`, etc.
- `Delta.cs`: default date set to prevent exceptions in edge cases
