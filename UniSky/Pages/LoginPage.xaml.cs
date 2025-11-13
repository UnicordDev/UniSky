using Microsoft.Extensions.DependencyInjection;
using UniSky.Services;
using UniSky.ViewModels;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Authentication.Web.Provider;
using Windows.Security.Credentials;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace UniSky.Pages;

public sealed partial class LoginPage : Page
{
    public LoginViewModel ViewModel
    {
        get => (LoginViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register("ViewModel", typeof(LoginViewModel), typeof(LoginPage), new PropertyMetadata(null));

    public LoginPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        this.Frame.BackStack.Clear();

        var safeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();
        safeAreaService.SetTitlebarTheme(ElementTheme.Default);
        safeAreaService.SafeAreaUpdated += OnSafeAreaUpdated;

        

        if (e.Parameter is not LoginViewModel vm)
            return;

        DataContext = ViewModel = vm;
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);
    }
    
    private void OnSafeAreaUpdated(object sender, SafeAreaUpdatedEventArgs e)
    {
        Margin = new Thickness(e.SafeArea.Bounds.Left, 0, e.SafeArea.Bounds.Right, e.SafeArea.Bounds.Bottom);
    }

    public bool IsNotNull(object o) 
        => o is not null;

    public bool Is(bool b)
        => b;
}
