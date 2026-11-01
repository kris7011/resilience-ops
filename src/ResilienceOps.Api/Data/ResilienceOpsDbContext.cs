using Microsoft.EntityFrameworkCore;
using ResilienceOps.Api.Features.Incidents;
using ResilienceOps.Api.Features.Mitigations;
using ResilienceOps.Api.Features.Risks;

namespace ResilienceOps.Api.Data;

public sealed class ResilienceOpsDbContext(
    DbContextOptions<ResilienceOpsDbContext> options)
    : DbContext(options)
{
    public DbSet<RiskItem> Risks =>
        Set<RiskItem>();

    public DbSet<MitigationAction> MitigationActions =>
        Set<MitigationAction>();

    public DbSet<Incident> Incidents =>
        Set<Incident>();

    public DbSet<IncidentMitigationLink>
        IncidentMitigationLinks =>
            Set<IncidentMitigationLink>();

    public DbSet<IncidentHistoryEntry>
        IncidentHistory =>
            Set<IncidentHistoryEntry>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureRisks(modelBuilder);
        ConfigureMitigations(modelBuilder);
        ConfigureIncidents(modelBuilder);
        ConfigureIncidentMitigationLinks(modelBuilder);
        ConfigureIncidentHistory(modelBuilder);
    }

    private static void ConfigureRisks(
        ModelBuilder modelBuilder)
    {
        var risk =
            modelBuilder.Entity<RiskItem>();

        risk.ToTable("Risks");

        risk.HasKey(item => item.Id);

        risk.Property(item => item.Title)
            .HasMaxLength(120)
            .IsRequired();

        risk.Property(item => item.Description)
            .HasMaxLength(1000)
            .IsRequired();

        risk.Property(item => item.Severity)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        risk.Property(item => item.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        risk.Property(item => item.CreatedUtc)
            .HasConversion(
                value =>
                    value.ToUnixTimeMilliseconds(),
                value =>
                    DateTimeOffset
                        .FromUnixTimeMilliseconds(value))
            .IsRequired();

        risk.HasData(
            new
            {
                Id = Guid.Parse(
                    "e0894902-f420-4f75-bf42-d9678de1fb5d"),
                Title =
                    "Identity provider outage",
                Description =
                    "Loss of the identity provider could prevent employees from accessing critical systems.",
                Severity =
                    RiskSeverity.Critical,
                Status =
                    RiskStatus.Open,
                CreatedUtc =
                    new DateTimeOffset(
                        2026,
                        7,
                        19,
                        7,
                        0,
                        0,
                        TimeSpan.Zero)
            },
            new
            {
                Id = Guid.Parse(
                    "82a22a45-56b0-437e-b65c-cc6d94e99e14"),
                Title =
                    "Delayed incident escalation",
                Description =
                    "Unclear ownership may delay escalation of high-impact operational incidents.",
                Severity =
                    RiskSeverity.High,
                Status =
                    RiskStatus.Open,
                CreatedUtc =
                    new DateTimeOffset(
                        2026,
                        7,
                        19,
                        7,
                        5,
                        0,
                        TimeSpan.Zero)
            });
    }

    private static void ConfigureMitigations(
        ModelBuilder modelBuilder)
    {
        var mitigation =
            modelBuilder.Entity<MitigationAction>();

        mitigation.ToTable(
            "MitigationActions");

        mitigation.HasKey(
            item => item.Id);

        mitigation.Property(
                item => item.Description)
            .HasMaxLength(500)
            .IsRequired();

        mitigation.Property(
                item => item.Owner)
            .HasMaxLength(120)
            .IsRequired();

        mitigation.Property(
                item => item.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        mitigation.Property(
                item => item.DueDateUtc)
            .HasConversion(
                value =>
                    value.ToUnixTimeMilliseconds(),
                value =>
                    DateTimeOffset
                        .FromUnixTimeMilliseconds(value))
            .IsRequired();

        mitigation.Property(
                item => item.CreatedUtc)
            .HasConversion(
                value =>
                    value.ToUnixTimeMilliseconds(),
                value =>
                    DateTimeOffset
                        .FromUnixTimeMilliseconds(value))
            .IsRequired();

        mitigation
            .HasOne<RiskItem>()
            .WithMany()
            .HasForeignKey(
                item => item.RiskId)
            .OnDelete(
                DeleteBehavior.Cascade);

        mitigation.HasIndex(
            item => item.RiskId);

        mitigation.HasIndex(
            item => new
            {
                item.RiskId,
                item.Status
            });
    }

    private static void ConfigureIncidents(
        ModelBuilder modelBuilder)
    {
        var incident =
            modelBuilder.Entity<Incident>();

        incident.ToTable("Incidents");

        incident.HasKey(item => item.Id);

        incident.Property(item => item.Title)
            .HasMaxLength(120)
            .IsRequired();

        incident.Property(item => item.Description)
            .HasMaxLength(1000)
            .IsRequired();

        incident.Property(
                item => item.ResponsiblePerson)
            .HasMaxLength(120)
            .IsRequired();

        incident.Property(item => item.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        incident.Property(item => item.ReportedUtc)
            .HasConversion(
                value =>
                    value.ToUnixTimeMilliseconds(),
                value =>
                    DateTimeOffset
                        .FromUnixTimeMilliseconds(value))
            .IsRequired();

        incident.Property(item => item.InvestigatingUtc)
            .HasConversion(
                value =>
                    value.HasValue
                        ? value.Value
                            .ToUnixTimeMilliseconds()
                        : (long?)null,
                value =>
                    value.HasValue
                        ? DateTimeOffset
                            .FromUnixTimeMilliseconds(
                                value.Value)
                        : null);

        incident.Property(item => item.ResolvedUtc)
            .HasConversion(
                value =>
                    value.HasValue
                        ? value.Value
                            .ToUnixTimeMilliseconds()
                        : (long?)null,
                value =>
                    value.HasValue
                        ? DateTimeOffset
                            .FromUnixTimeMilliseconds(
                                value.Value)
                        : null);

        incident.Property(item => item.ClosedUtc)
            .HasConversion(
                value =>
                    value.HasValue
                        ? value.Value
                            .ToUnixTimeMilliseconds()
                        : (long?)null,
                value =>
                    value.HasValue
                        ? DateTimeOffset
                            .FromUnixTimeMilliseconds(
                                value.Value)
                        : null);

        incident.Property(item => item.UpdatedUtc)
            .HasConversion(
                value =>
                    value.ToUnixTimeMilliseconds(),
                value =>
                    DateTimeOffset
                        .FromUnixTimeMilliseconds(value))
            .IsRequired();

        incident.HasIndex(
            item =>
                new
                {
                    item.RiskId,
                    item.Status
                });

        incident.HasIndex(
            item =>
                new
                {
                    item.RiskId,
                    item.ReportedUtc
                });

        incident.HasOne<RiskItem>()
            .WithMany()
            .HasForeignKey(item => item.RiskId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureIncidentMitigationLinks(
        ModelBuilder modelBuilder)
    {
        var link =
            modelBuilder.Entity<
                IncidentMitigationLink>();

        link.ToTable("IncidentMitigations");

        link.HasKey(
            item =>
                new
                {
                    item.IncidentId,
                    item.MitigationId
                });

        link.HasIndex(item => item.MitigationId);

        link.HasOne<Incident>()
            .WithMany(
                incident =>
                    incident.MitigationLinks)
            .HasForeignKey(item => item.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        link.HasOne<MitigationAction>()
            .WithMany()
            .HasForeignKey(item => item.MitigationId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureIncidentHistory(
        ModelBuilder modelBuilder)
    {
        var historyEntry =
            modelBuilder.Entity<
                IncidentHistoryEntry>();

        historyEntry.ToTable("IncidentHistory");

        historyEntry.HasKey(item => item.Id);

        // The domain assigns the id. Without this, EF Core treats a new
        // entry added to a loaded incident as an existing row and issues an
        // UPDATE instead of an INSERT.
        historyEntry.Property(item => item.Id)
            .ValueGeneratedNever();

        historyEntry.Property(item => item.Sequence)
            .IsRequired();

        historyEntry.Property(
                item => item.PreviousStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        historyEntry.Property(item => item.NewStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        historyEntry.Property(
                item => item.OccurredUtc)
            .HasConversion(
                value =>
                    value.ToUnixTimeMilliseconds(),
                value =>
                    DateTimeOffset
                        .FromUnixTimeMilliseconds(value))
            .IsRequired();

        historyEntry.HasIndex(
                item =>
                    new
                    {
                        item.IncidentId,
                        item.Sequence
                    })
            .IsUnique();

        historyEntry.HasOne<Incident>()
            .WithMany(
                incident =>
                    incident.History)
            .HasForeignKey(item => item.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
