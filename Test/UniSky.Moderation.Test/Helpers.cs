namespace UniSky.Moderation.Test;

internal static class Helpers
{
    internal static void AssertModerationUI(
        ModerationUI ui,
        bool alert = false,
        bool blur = false, 
        bool filter = false, 
        bool inform = false, 
        bool noOverride = false)
    {
        Assert.Equal(alert, ui.Alert);
        Assert.Equal(blur, ui.Blur);
        Assert.Equal(filter, ui.Filter);
        Assert.Equal(inform, ui.Inform);
        Assert.Equal(noOverride, ui.NoOverride);
    }
}