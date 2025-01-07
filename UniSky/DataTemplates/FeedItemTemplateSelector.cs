using UniSky.ViewModels.Posts;
using UniSky.ViewModels.Profile;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniSky.DataTemplates;

public class FeedItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate FeedPostTemplate { get; set; }
    public DataTemplate FeedProfileTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            PostViewModel => FeedPostTemplate,
            ProfileViewModel => FeedProfileTemplate,
            _ => null,
        };
    }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return SelectTemplateCore(item, null);
    }
}
