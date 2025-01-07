using Microsoft.Toolkit.Uwp.UI.Extensions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace UniSky.Controls;

[ContentProperty(Name = nameof(Content))]
public sealed class SpoilerControl : Control
{
    public bool IsHidden
    {
        get => (bool)GetValue(IsHiddenProperty);
        set => SetValue(IsHiddenProperty, value);
    }

    public static readonly DependencyProperty IsHiddenProperty =
        DependencyProperty.Register("IsHidden", typeof(bool), typeof(SpoilerControl), new PropertyMetadata(false, OnDependantPropertyChanged));

    public object Content
    {
        get => (object)GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register("Content", typeof(object), typeof(SpoilerControl), new PropertyMetadata(DependencyProperty.UnsetValue));

    public DataTemplate ContentTemplate
    {
        get => (DataTemplate)GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    public static readonly DependencyProperty ContentTemplateProperty =
        DependencyProperty.Register("ContentTemplate", typeof(DataTemplate), typeof(SpoilerControl), new PropertyMetadata(DependencyProperty.UnsetValue));

    public DataTemplateSelector ContentTemplateSelector
    {
        get => (DataTemplateSelector)GetValue(ContentTemplateSelectorProperty);
        set => SetValue(ContentTemplateSelectorProperty, value);
    }

    public static readonly DependencyProperty ContentTemplateSelectorProperty =
        DependencyProperty.Register("ContentTemplateSelector", typeof(DataTemplateSelector), typeof(SpoilerControl), new PropertyMetadata(DependencyProperty.UnsetValue));

    public object Warning
    {
        get => (object)GetValue(WarningProperty);
        set => SetValue(WarningProperty, value);
    }

    public static readonly DependencyProperty WarningProperty =
        DependencyProperty.Register("Warning", typeof(object), typeof(SpoilerControl), new PropertyMetadata(DependencyProperty.UnsetValue, OnDependantPropertyChanged));

    public DataTemplate WarningTemplate
    {
        get => (DataTemplate)GetValue(WarningTemplateProperty);
        set => SetValue(WarningTemplateProperty, value);
    }

    public static readonly DependencyProperty WarningTemplateProperty =
        DependencyProperty.Register("WarningTemplate", typeof(DataTemplate), typeof(SpoilerControl), new PropertyMetadata(DependencyProperty.UnsetValue));

    public DataTemplateSelector WarningTemplateSelector
    {
        get => (DataTemplateSelector)GetValue(WarningTemplateSelectorProperty);
        set => SetValue(WarningTemplateSelectorProperty, value);
    }

    public static readonly DependencyProperty WarningTemplateSelectorProperty =
        DependencyProperty.Register("WarningTemplateSelector", typeof(DataTemplateSelector), typeof(SpoilerControl), new PropertyMetadata(DependencyProperty.UnsetValue));

    public bool CanOverride
    {
        get { return (bool)GetValue(CanOverrideProperty); }
        set { SetValue(CanOverrideProperty, value); }
    }

    public static readonly DependencyProperty CanOverrideProperty =
        DependencyProperty.Register("CanOverride", typeof(bool), typeof(SpoilerControl), new PropertyMetadata(true));

    public SpoilerControl()
    {
        this.DefaultStyleKey = typeof(SpoilerControl);
    }

    private static void OnDependantPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((SpoilerControl)d).UpdateStates();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        this.UpdateStates();

        var warningButton = (Button)this.FindDescendantByName("PART_WarningButton");
        warningButton.Click += WarningButton_Click;
    }

    private void WarningButton_Click(object sender, RoutedEventArgs e)
    {
        IsHidden = !IsHidden;
    }

    private void UpdateStates()
    {
        if (Warning == null)
        {
            VisualStateManager.GoToState(this, "NoWarning", true);
        }
        else if (IsHidden)
        {
            VisualStateManager.GoToState(this, "Hidden", true);
        }
        else
        {
            VisualStateManager.GoToState(this, "Shown", true);
        }
    }
}
