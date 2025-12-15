using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using UniSky.Models;

namespace UniSky.Notifications.Data;

[Index(nameof(Did))]
[Index(nameof(ChannelUrl), IsUnique = true)]
public class NotificationRegistration
{
    public string Did { get; set; } = null!;
    public string InstallId { get; set; } = null!;
    public string ChannelUrl { get; set; } = null!;
    public NotificationOptions Options { get; set; } = (NotificationOptions)0;
}

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions options) : base(options)
    {
        Registrations = Set<NotificationRegistration>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<NotificationRegistration>()
           .HasKey(r => new { r.Did, r.InstallId });
    }

    public DbSet<NotificationRegistration> Registrations { get; set; }
}
