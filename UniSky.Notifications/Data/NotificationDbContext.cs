using Microsoft.EntityFrameworkCore;

namespace UniSky.Notifications.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions options) : base(options)
    {
    }
}
