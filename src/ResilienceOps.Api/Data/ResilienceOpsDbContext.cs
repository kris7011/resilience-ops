using Microsoft.EntityFrameworkCore;
using ResilienceOps.Api.Features.Risks;

namespace ResilienceOps.Api.Data;

public sealed class ResilienceOpsDbContext(
    DbContextOptions<ResilienceOpsDbContext> options)
    : DbContext(options)
{
    public DbSet<RiskItem> Risks => Set<RiskItem>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var risk = modelBuilder.Entity<RiskItem>();

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
                value => value.ToUnixTimeMilliseconds(),
                value => DateTimeOffset.FromUnixTimeMilliseconds(value))
            .IsRequired();

        risk.HasData(
            new
            {
                Id = Guid.Parse(
                    "e0894902-f420-4f75-bf42-d9678de1fb5d"),
                Title = "Identity provider outage",
                Description =
                    "Loss of the identity provider could prevent employees from accessing critical systems.",
                Severity = RiskSeverity.Critical,
                Status = RiskStatus.Open,
                CreatedUtc = new DateTimeOffset(
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
                Title = "Delayed incident escalation",
                Description =
                    "Unclear ownership may delay escalation of high-impact operational incidents.",
                Severity = RiskSeverity.High,
                Status = RiskStatus.Open,
                CreatedUtc = new DateTimeOffset(
                    2026,
                    7,
                    19,
                    7,
                    5,
                    0,
                    TimeSpan.Zero)
            });
    }
}