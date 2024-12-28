using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using UniSky.ViewModels.Thread;

namespace UniSky.DataTemplates;

public class ThreadItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate ThreadPostTemplate { get; set; }
    public DataTemplate ThreadHighlightedPostTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            ThreadPostViewModel { IsSelected: false } => ThreadPostTemplate,
            ThreadPostViewModel { IsSelected: true } => ThreadHighlightedPostTemplate,
            _ => null,
        };
    }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return SelectTemplateCore(item, null);
    }
}
