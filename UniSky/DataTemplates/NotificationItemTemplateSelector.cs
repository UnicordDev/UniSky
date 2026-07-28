using UniSky.ViewModels.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniSky.DataTemplates;

public class NotificationItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate PostNotificationTemplate { get; set; }
    public DataTemplate AggregateNotificationTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            PostNotificationViewModel => PostNotificationTemplate,
            AggregateNotificationViewModel => AggregateNotificationTemplate,
            _ => null,
        };
    }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return SelectTemplateCore(item, null);
    }
}
