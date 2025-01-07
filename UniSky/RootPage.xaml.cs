using Microsoft.Extensions.DependencyInjection;
using UniSky.Helpers.Composition;
using UniSky.Services;
using UniSky.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Core;

namespace UniSky;

public sealed partial class RootPage : Page
{
    private bool dismissed;

    public RootViewModel ViewModel
    {
        get => (RootViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register("ViewModel", typeof(RootViewModel), typeof(RootPage), new PropertyMetadata(null));

    public RootPage()
    {
        this.InitializeComponent();
        this.DataContext = this.ViewModel = ActivatorUtilities.CreateInstance<RootViewModel>(ServiceContainer.Scoped);
    }

    private void RootFrame_Navigated(object sender, NavigationEventArgs e)
    {
        Dismiss();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();

        var serviceLocator = ServiceContainer.Scoped.GetRequiredService<INavigationServiceLocator>();
        var service = serviceLocator.GetNavigationService("Root");
        service.Frame = RootFrame;
    }

    void Dismiss()
    {
        _ = Dispatcher.RunIdleAsync((a) =>
        {
            if (dismissed)
                return;

            dismissed = true;
            ExtendedProgressRing.IsActive = false;
            BirdAnimation.RunBirdAnimation(ExtendedSplash, SheetRoot);
        });
    }
}
