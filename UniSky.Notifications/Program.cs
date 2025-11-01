using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using UniSky.Notifications.Data;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddDbContext<NotificationDbContext>(s => s.UseSqlite("Data Source=notifications.db"));

var app = builder.Build();

var notificationsApi = app.MapGroup("/push");
notificationsApi.MapPost("/register/{id}", (int id) =>
{
    

    return Results.NoContent();
});

app.Run();

public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

[JsonSerializable(typeof(Todo[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
