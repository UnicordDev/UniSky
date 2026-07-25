namespace UniSky.Notifications.Models;

public class ClassicNotificationBuilder
{
    public string? Title { get; set; }
    public string? Content { get; set; }

    public ClassicNotificationBuilder SetTitle(string title)
    {
        this.Title = title;
        return this;
    }
    public ClassicNotificationBuilder SetContent(string content)
    {
        this.Content = content;
        return this;
    }
}
