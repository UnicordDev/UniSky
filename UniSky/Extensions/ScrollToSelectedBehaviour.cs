using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;

namespace UniSky.Extensions;

public static class ScrollToSelectedBehavior
{
    public static readonly DependencyProperty SelectedValueProperty
        = DependencyProperty.RegisterAttached("SelectedValue", typeof(object), typeof(ScrollToSelectedBehavior), new PropertyMetadata(null, OnSelectedValueChange));

    public static void SetSelectedValue(DependencyObject source, object value)
    {
        source.SetValue(SelectedValueProperty, value);
    }

    public static object GetSelectedValue(DependencyObject source)
    {
        return (object)source.GetValue(SelectedValueProperty);
    }

    private static void OnSelectedValueChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ListViewBase listView)
            listView.ScrollIntoView(e.NewValue, ScrollIntoViewAlignment.Leading);
    }
}