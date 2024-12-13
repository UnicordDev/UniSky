using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniSky.Services;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace UniSky.Controls.Overlay;

[ContentProperty(Name = nameof(OverlayContent))]
public abstract class OverlayControl : Control
{
    public static readonly DependencyProperty OverlayContentProperty =
        DependencyProperty.Register("OverlayContent", typeof(object), typeof(OverlayControl), new PropertyMetadata(null));

    public static readonly DependencyProperty OverlayContentTemplateProperty =
        DependencyProperty.Register("OverlayContentTemplate", typeof(DataTemplate), typeof(OverlayControl), new PropertyMetadata(null));

    public static readonly DependencyProperty TitleContentProperty =
        DependencyProperty.Register("TitleContent", typeof(object), typeof(OverlayControl), new PropertyMetadata(null));

    public static readonly DependencyProperty TitleContentTemplateProperty =
        DependencyProperty.Register("TitleContentTemplate", typeof(DataTemplate), typeof(OverlayControl), new PropertyMetadata(null));

    public static readonly DependencyProperty PreferredWindowSizeProperty =
        DependencyProperty.Register("PreferredWindowSize", typeof(Size), typeof(OverlayControl), new PropertyMetadata(new Size(320, 400)));

    public IOverlayController Controller { get; private set; }

    public object OverlayContent
    {
        get => (object)GetValue(OverlayContentProperty);
        set => SetValue(OverlayContentProperty, value);
    }

    public DataTemplate OverlayContentTemplate
    {
        get => (DataTemplate)GetValue(OverlayContentTemplateProperty);
        set => SetValue(OverlayContentTemplateProperty, value);
    }

    public object TitleContent
    {
        get => (object)GetValue(TitleContentProperty);
        set => SetValue(TitleContentProperty, value);
    }

    public DataTemplate TitleContentTemplate
    {
        get => (DataTemplate)GetValue(TitleContentTemplateProperty);
        set => SetValue(TitleContentTemplateProperty, value);
    }

    public Size PreferredWindowSize
    {
        get => (Size)GetValue(PreferredWindowSizeProperty);
        set => SetValue(PreferredWindowSizeProperty, value);
    }

    public event TypedEventHandler<OverlayControl, RoutedEventArgs> Hidden;
    public event TypedEventHandler<OverlayControl, OverlayHidingEventArgs> Hiding;
    public event TypedEventHandler<OverlayControl, OverlayShowingEventArgs> Showing;
    public event TypedEventHandler<OverlayControl, RoutedEventArgs> Shown;

    internal void SetOverlayController(IOverlayController controller)
    {
        Controller = controller;
    }

    internal void InvokeHidden()
    {
        OnHidden(new RoutedEventArgs());
    }

    internal async Task<bool> InvokeHidingAsync()
    {
        var ev = new OverlayHidingEventArgs();
        OnHiding(ev);

        await ev.WaitOnDeferral();

        return !ev.Cancel;
    }

    internal void InvokeShowing(object parameter)
    {
        OnShowing(new OverlayShowingEventArgs(parameter));
    }

    internal void InvokeShown()
    {
        OnShown(new RoutedEventArgs());
    }

    protected virtual void OnHiding(OverlayHidingEventArgs args)
    {
        Hiding?.Invoke(this, args);
    }

    protected virtual void OnHidden(RoutedEventArgs args)
    {
        Hidden?.Invoke(this, args);
    }

    protected virtual void OnShowing(OverlayShowingEventArgs args)
    {
        Showing?.Invoke(this, args);
    }

    protected virtual void OnShown(RoutedEventArgs args)
    {
        Shown?.Invoke(this, args);
    }
}
