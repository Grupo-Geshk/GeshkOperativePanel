// Data/ApplicationDbContext.cs
using ControlPanelGeshk.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ControlPanelGeshk.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> opts) : base(opts) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<Credential> Credentials => Set<Credential>();
    public DbSet<CredentialAccessLog> CredentialAccessLogs => Set<CredentialAccessLog>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteItem> QuoteItems => Set<QuoteItem>();
    public DbSet<EconomicsPlan> EconomicsPlans => Set<EconomicsPlan>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Índices clave
        b.Entity<Client>().HasIndex(x => x.BusinessName);
        b.Entity<Client>().HasIndex(x => x.ClientName);
        b.Entity<Project>().HasIndex(x => new { x.ClientId, x.Status, x.BillingType });
        b.Entity<Project>().HasIndex(x => x.DeliveredAt);
        b.Entity<Meeting>().HasIndex(x => new { x.ScheduledAt, x.Status });
        b.Entity<Transaction>().HasIndex(x => x.Date);
        b.Entity<Transaction>().HasIndex(x => new { x.Type, x.Category });
        b.Entity<Transaction>().HasIndex(x => new { x.ClientId, x.ProjectId });

        // JSONB
        b.Entity<Activity>().Property(x => x.Payload).HasColumnType("jsonb");
        b.Entity<AuditLog>().Property(x => x.Diff).HasColumnType("jsonb");

        // Relaciones y cascadas suaves
        b.Entity<Project>()
            .HasOne(p => p.Client).WithMany(c => c.Projects)
            .HasForeignKey(p => p.ClientId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<QuoteItem>()
            .HasOne(i => i.Quote).WithMany(q => q.Items)
            .HasForeignKey(i => i.QuoteId).OnDelete(DeleteBehavior.Cascade);
    }
}
