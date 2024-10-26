using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.DependencyInjection;
using FishyFlip.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Media;
using UniSky.Services;
using UniSky.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using MUXC = Microsoft.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UniSky.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        public HomeViewModel ViewModel
        {
            get { return (HomeViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(HomeViewModel), typeof(HomePage), new PropertyMetadata(null));

        public HomePage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var serviceLocator = Ioc.Default.GetRequiredService<INavigationServiceLocator>();
            var service = serviceLocator.GetNavigationService("Home");
            service.Frame = NavigationFrame;

            if (e.Parameter is string session || e.Parameter is ATDid did && (session = did.Handler) != null)
            {
                ViewModel = ActivatorUtilities.CreateInstance<HomeViewModel>(Ioc.Default, session);
            }
        }

        private void NavigationView_ItemInvoked(MUXC.NavigationView sender, MUXC.NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer.Tag is HomePages page)
                ViewModel.Page = page;
        }

        private void FooterToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            var button = (ToggleButton)sender;
            var page = (HomePages)button.Tag;

            ViewModel.Page = page;
        }

        private void FooterToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ViewModel.UpdateChecked();
        }
    }
}
