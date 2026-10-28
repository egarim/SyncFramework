# Changelog

All notable changes to SyncFramework are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [Unreleased]

### Added
- **`SyncFrameworkInMemoryClient`** (`BIT.Data.Sync.Client`): an `ISyncFrameworkClient` that talks to an `ISyncFrameworkServer` in the same process, with no HTTP. It answers Push, Fetch and HandShake as `SyncControllerBase` does and copies deltas at the boundary, as serialization would. For tests, simulations and apps that host their own server.
- `InMemorySyncServerTests`: three databases converge through an in-memory server; updates and deletes propagate; pushing never renumbers a client's own deltas; an unknown node fails with a message.

### Changed
- Packages updated to the latest stable .NET 10 releases: EF Core 10.0.14, Npgsql EF 10.0.3, MySql.EntityFrameworkCore 10.0.12, ASP.NET Core 10.0.14, test SDK 18.11.0, NUnit 3.14.0 (NUnit 4+ would need the classic asserts rewritten), MudBlazor 8.15.0.
- `BIT.Data.Sync.AspNetCore` uses the shared framework (`Microsoft.AspNetCore.App`) instead of `Microsoft.AspNetCore.Mvc.Core` 2.2.5.
- **Breaking (binary):** `SimpleDatabase.Update` and `Delete` return `Task` instead of `async void`, so callers can await them and see their exceptions. Source-compatible; already-compiled callers of these two methods must be rebuilt.
- The cross-database EF Core tests are skipped, with the missing connection strings named, instead of failing when SQL Server, PostgreSQL and MySQL are not available.

### Removed
- **`BIT.Data.Sync.EfCore.Pomelo.MySql`**, finishing the move to the official MySQL driver that 10.0.0 started: Pomelo has no EF Core 10 release. The playground and the Studio now use `MySqlDeltaGenerator` from `BIT.Data.Sync.EfCore.MySql`, which is also now in `All.sln`. The `MySqlPomelo` updater alias stays so deltas stored by Pomelo clients remain readable.

### Fixed
- `MemoryDeltaStore`: a client that pulled before its first push had its push cursor set to the server's index, which counts ahead of the client's own, so its deltas were never pushed. A pull now sets only the pull cursor, and an identity with no push yet reads as nothing pushed. Found while syncing Creatura's work ledger; covered by `ADatabaseThatPullsBeforeItsFirstPushStillPushesAllItsDeltas`.
- `SyncFrameworkServer`: the explicit `ISyncFrameworkServer.RegisterNodeFunction` threw `NotImplementedException`, so the server failed when used through its interface.
- `SyncFrameworkHttpClient.PushAsync` returned `null` on a non-success HTTP status; it now returns a failed response with the status and body.
- `MemoryDeltaStore`: a push moved the pull cursor to the client's own push index, so after pushing a client skipped other clients' deltas with lower server indexes; and the pull cursor came back `null` for a status created by a push.
- `SyncControllerBase.Push` returned `Success = false` with no message for three kinds of exception.
- The solution did not build: `SynFrameworkStudio.Module` failed its restore with NU1605 (SqlClient, Azure.Identity, IdentityModel.Tokens.Jwt downgrades).
- Release builds failed on macOS, Linux and CI: each package's `CopyPackage` step copied to `$(USERPROFILE)\Documents\MyNugets`, which is `/Documents` where `USERPROFILE` is unset. The step now runs only when `USERPROFILE` is set, so the Windows local feed works as before.

### Security
- No known vulnerable package remains, transitive included. Removed or overridden: Newtonsoft.Json 9.0.1, SQLitePCLRaw.lib.e_sqlite3 2.1.11, System.Security.Cryptography.Pkcs 8.0.1 and 9.0.x, System.Security.Cryptography.Xml 8.0.2, MessagePack 3.1.4.

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
