using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Extensions.DependencyInjection;
using UniSky.Areas.WebAccountManager.ViewModels;
using UniSky.Helpers.Composition;
using UniSky.Pages;
using UniSky.Services;
using UniSky.ViewModels.Notifications;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UniSky.Areas.WebAccountManager.Pages;

public sealed partial class AccountManagerAddAccountPage : Page
{
    public AccountManagerAddAccountViewModel ViewModel
    {
        get => (AccountManagerAddAccountViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register("ViewModel", typeof(AccountManagerAddAccountViewModel), typeof(AccountManagerAddAccountPage), new PropertyMetadata(null));

    public AccountManagerAddAccountPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var safeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();
        safeAreaService.SetTitlebarTheme(ElementTheme.Default);
        safeAreaService.SafeAreaUpdated += OnSafeAreaUpdated;

        if (this.ViewModel == null)
            this.DataContext = this.ViewModel = ActivatorUtilities.CreateInstance<AccountManagerAddAccountViewModel>(ServiceContainer.Scoped, (WebAccountProviderActivatedEventArgs)e.Parameter);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        var safeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();
        safeAreaService.SafeAreaUpdated -= OnSafeAreaUpdated;
    }

    private void OnSafeAreaUpdated(object sender, SafeAreaUpdatedEventArgs e)
    {
        var themeService = ServiceContainer.Scoped.GetRequiredService<IThemeService>();
        TitleBarPadding.Height = new GridLength(e.SafeArea.Bounds.Top);
    }

    private async void HandleTextBox_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            e.Handled = true;

            await ViewModel.BeginLoginAsync();
        }
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.BeginLoginAsync();
    }
}
