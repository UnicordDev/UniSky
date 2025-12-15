using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UniSky.Notifications;
using UniSky.Notifications.Data;
using UniSky.Notifications.Services;
using UniSky.Notifications.Services.Providers;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Configuration.AddIniFile("secrets.ini");

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddDbContext<NotificationDbContext>(s => s.UseSqlite("Data Source=notifications.db"));

builder.Services.AddHostedService<SpacedustService>();
builder.Services.AddHostedService<PushService>();

builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();

builder.Services.AddNotificationProviders();

var app = builder.Build();

ApplyMigrations(app);

app.MapNotifications();

app.Run();

static void ApplyMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    using var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

    dbContext.Database.Migrate();
}