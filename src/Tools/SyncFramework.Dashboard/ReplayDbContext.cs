using Microsoft.EntityFrameworkCore;

namespace SyncFramework.Dashboard;

public class ReplayDbContext : DbContext
{
    public ReplayDbContext(DbContextOptions<ReplayDbContext> options) : base(options) { }

    public DbSet<ReplayedDeltaRecord> ReplayedDeltas => Set<ReplayedDeltaRecord>();
}

public class ReplayedDeltaRecord
{
    public int Id { get; set; }
    public string DeltaId { get; set; } = "";
    public string Index { get; set; } = "";
    public string? Identity { get; set; }
    public int OperationSize { get; set; }
    public string? OperationBase64 { get; set; }
}
