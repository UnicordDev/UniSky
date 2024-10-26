using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using UniSky.ViewModels;
using UniSky.ViewModels.Feeds;
using Uno.UI;
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
    public sealed partial class FeedsPage : Page
    {
        public FeedsViewModel ViewModel
        {
            get { return (FeedsViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(FeedsViewModel), typeof(FeedsPage), new PropertyMetadata(null));

        public FeedsPage()
        {
            this.ViewModel = ActivatorUtilities.CreateInstance<FeedsViewModel>(Ioc.Default);
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        private async void OnRefreshRequested(MUXC.RefreshContainer sender, MUXC.RefreshRequestedEventArgs args)
        {
            var feed = (FeedViewModel)sender.DataContext;

            var deferral = args.GetDeferral();
            await feed.RefreshAsync(deferral);
        }
    }
}
