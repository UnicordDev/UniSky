using System.Windows.Input;
using UniSky.ViewModels.Error;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniSky.Controls;

public sealed partial class LoadErrorControl : UserControl
{
    public LoadErrorControl()
    {
        this.InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(LoadErrorControl), new PropertyMetadata(null));

    public ErrorViewModel Error
    {
        get => (ErrorViewModel)GetValue(ErrorProperty);
        set => SetValue(ErrorProperty, value);
    }

    public static readonly DependencyProperty ErrorProperty =
        DependencyProperty.Register(nameof(Error), typeof(ErrorViewModel), typeof(LoadErrorControl),
            new PropertyMetadata(null, OnErrorChanged));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        private set => SetValue(MessageProperty, value);
    }

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(LoadErrorControl), new PropertyMetadata(null));

    public ICommand RetryCommand
    {
        get => (ICommand)GetValue(RetryCommandProperty);
        set => SetValue(RetryCommandProperty, value);
    }

    public static readonly DependencyProperty RetryCommandProperty =
        DependencyProperty.Register(nameof(RetryCommand), typeof(ICommand), typeof(LoadErrorControl), new PropertyMetadata(null));

    private static readonly ResourceLoader strings = ResourceLoader.GetForViewIndependentUse();

    private static void OnErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (LoadErrorControl)d;
        var error = e.NewValue as ErrorViewModel;

        control.Message = string.IsNullOrWhiteSpace(error?.Message)
            ? strings.GetString("LoadError_Unknown")
            : error.Message;

        control.Visibility = error is null ? Visibility.Collapsed : Visibility.Visible;
    }
}
