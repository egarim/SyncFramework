# Database Synchronization Test Flow

This document illustrates the data flow in the `AllDatabaseTests.cs` test that validates cross-database synchronization using a master-node architecture.

## System Architecture

```mermaid
graph TB
    Master["🏛️ Master Node<br/>SQL Server<br/>Primary Database"]
    SyncServer["🔄 Sync Server<br/>HTTP API<br/>Coordination Hub"]

    NodeA["📱 Node A<br/>SQLite<br/>Local Database"]
    NodeB["🐘 Node B<br/>PostgreSQL<br/>Remote Database"]
    NodeC["🐬 Node C<br/>MySQL<br/>Cloud Database"]

    Master <--> SyncServer
    NodeA <--> SyncServer
    NodeB <--> SyncServer
    NodeC <--> SyncServer

    classDef master fill:#e3f2fd,stroke:#1976d2,stroke-width:3px
    classDef sync fill:#fce4ec,stroke:#c2185b,stroke-width:3px
    classDef nodeA fill:#f3e5f5,stroke:#7b1fa2,stroke-width:2px
    classDef nodeB fill:#e8f5e8,stroke:#388e3c,stroke-width:2px
    classDef nodeC fill:#fff3e0,stroke:#f57c00,stroke-width:2px

    class Master master
    class SyncServer sync
    class NodeA nodeA
    class NodeB nodeB
    class NodeC nodeC
```

## Step 1: Initial Data Distribution

The master creates initial data and distributes it to all nodes.

```mermaid
sequenceDiagram
    participant M as Master (SQL Server)
    participant S as Sync Server
    participant A as Node A (SQLite)
    participant B as Node B (PostgreSQL)
    participant C as Node C (MySQL)

    Note over M: Create 4 Blogs with Posts
    M->>M: Add "SqlServer blog"
    M->>M: Add "Sqlite blog"
    M->>M: Add "Npgsql blog"
    M->>M: Add "Pomelo MySql blog"
    M->>M: SaveChanges()

    M->>S: Push() - Send all blogs
    Note over S: Store changes for distribution

    S->>A: Pull() - Receive 4 blogs
    S->>B: Pull() - Receive 4 blogs
    S->>C: Pull() - Receive 4 blogs

    Note over A,C: All nodes now have 4 blogs
```

**Result**: All nodes (Master, Node A, Node B, Node C) have 4 blogs each.

## Step 2: Local Data Creation

Each node creates its own local data independently.

```mermaid
flowchart TB
    subgraph "Local Data Creation"
        A1["Node A (SQLite)<br/>Creates 'Node A Blog'<br/>+ 2 Posts"]
        B1["Node B (PostgreSQL)<br/>Creates 'Node B Blog'<br/>+ 2 Posts"]
        C1["Node C (MySQL)<br/>Creates 'Node C Blog'<br/>+ 2 Posts"]
    end

    A1 --> A2["Node A: 5 blogs total<br/>(4 synced + 1 local)"]
    B1 --> B2["Node B: 5 blogs total<br/>(4 synced + 1 local)"]
    C1 --> C2["Node C: 5 blogs total<br/>(4 synced + 1 local)"]

    classDef nodeA fill:#f3e5f5,stroke:#7b1fa2,stroke-width:2px
    classDef nodeB fill:#e8f5e8,stroke:#388e3c,stroke-width:2px
    classDef nodeC fill:#fff3e0,stroke:#f57c00,stroke-width:2px
    classDef result fill:#e8f5e8,stroke:#4caf50,stroke-width:2px

    class A1 nodeA
    class B1 nodeB
    class C1 nodeC
    class A2,B2,C2 result
```

**Result**: Each node has 5 blogs (4 original + 1 local).

## Step 3: Push Changes to Sync Server

All nodes push their local changes to the central sync server.

```mermaid
sequenceDiagram
    participant A as Node A (SQLite)
    participant B as Node B (PostgreSQL)
    participant C as Node C (MySQL)
    participant S as Sync Server

    Note over A,C: Each node has 1 new blog to sync

    A->>S: Push() - Send "Node A Blog"
    Note over S: Store Node A changes

    B->>S: Push() - Send "Node B Blog"
    Note over S: Store Node B changes

    C->>S: Push() - Send "Node C Blog"
    Note over S: Store Node C changes

    Note over S: Server now has 3 new blogs<br/>ready for distribution
```

**Result**: Sync server has collected 3 new blogs from the nodes.

## Step 4: Master Consolidation

The master pulls all changes from the sync server.

```mermaid
flowchart LR
    S["🔄 Sync Server<br/>Has 3 new blogs:<br/>- Node A Blog<br/>- Node B Blog<br/>- Node C Blog"]

    M["🏛️ Master Database<br/>Originally: 4 blogs"]

    S -->|"Pull() all changes"| M

    M --> R["✅ Master Result<br/>7 blogs total<br/>(4 original + 3 from nodes)"]

    classDef sync fill:#fce4ec,stroke:#c2185b,stroke-width:3px
    classDef master fill:#e3f2fd,stroke:#1976d2,stroke-width:3px
    classDef result fill:#e8f5e8,stroke:#4caf50,stroke-width:2px

    class S sync
    class M master
    class R result
```

**Result**: Master now has 7 blogs total.

## Step 5: Complete Synchronization

All nodes fetch and pull the complete dataset from the sync server.

```mermaid
sequenceDiagram
    participant S as Sync Server
    participant A as Node A (SQLite)
    participant B as Node B (PostgreSQL)
    participant C as Node C (MySQL)

    Note over S: Has complete dataset<br/>(7 blogs total)

    S->>A: Fetch() + Pull() - Get all 7 blogs
    Note over A: Now has all blogs from all nodes

    S->>B: Fetch() + Pull() - Get all 7 blogs
    Note over B: Now has all blogs from all nodes

    S->>C: Fetch() + Pull() - Get all 7 blogs
    Note over C: Now has all blogs from all nodes

    Note over A,C: All nodes synchronized<br/>Each has 7 blogs
```

**Result**: All nodes are fully synchronized with 7 blogs each.

## Step 6: Second Round of Changes

The test performs a second round to verify continued synchronization.

```mermaid
flowchart TB
    subgraph "Second Round Changes"
        A3["Node A adds<br/>another blog"]
        B3["Node B adds<br/>another blog"]
        C3["Node C adds<br/>another blog"]
    end

    A3 --> P["Push to Sync Server"]
    B3 --> P
    C3 --> P

    P --> M3["Master pulls<br/>3 more blogs"]

    subgraph "Final State"
        MR["Master: 10 blogs<br/>(7 + 3 new)"]
        NR["Each Node: 8 blogs<br/>(7 + 1 own new)"]
    end

    M3 --> MR
    P --> NR

    classDef change fill:#fff3e0,stroke:#f57c00,stroke-width:2px
    classDef sync fill:#fce4ec,stroke:#c2185b,stroke-width:2px
    classDef result fill:#e8f5e8,stroke:#4caf50,stroke-width:2px

    class A3,B3,C3 change
    class P sync
    class MR,NR result
```

**Final Result**: Master has 10 blogs, each node has 8 blogs.

## Summary

This test validates:

1. **Multi-database support** - SQL Server, SQLite, PostgreSQL, and MySQL
2. **Bidirectional synchronization** - Data flows both ways
3. **Conflict resolution** - Multiple nodes can make changes safely
4. **Data consistency** - All nodes eventually have consistent data
5. **Incremental sync** - Only changes are transmitted, not full datasets

The synchronization framework successfully handles complex scenarios with multiple database types and ensures data consistency across all nodes.
