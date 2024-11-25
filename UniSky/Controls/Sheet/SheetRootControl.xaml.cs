using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using UniSky.Pages;
using UniSky.Services;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using MUXC = Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UniSky.Controls.Sheet
{
    [ContentProperty(Name = nameof(ContentElement))]
    public sealed partial class SheetRootControl : UserControl
    {
        public FrameworkElement ContentElement
        {
            get { return (FrameworkElement)GetValue(ContentElementProperty); }
            set { SetValue(ContentElementProperty, value); }
        }

        public static readonly DependencyProperty ContentElementProperty =
            DependencyProperty.Register("ContentElement", typeof(FrameworkElement), typeof(SheetRootControl), new PropertyMetadata(null));

        public double TotalHeight
        {
            get { return (double)GetValue(TotalHeightProperty); }
            set { SetValue(TotalHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TotalHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TotalHeightProperty =
            DependencyProperty.Register("TotalHeight", typeof(double), typeof(SheetRootControl), new PropertyMetadata(0.0));

        public SheetRootControl()
        {
            this.InitializeComponent();
            VisualStateManager.GoToState(this, "Closed", false);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (finalSize.Width > 620)
                TotalHeight = 64;
            else
                TotalHeight = finalSize.Height;

            SheetRoot.Height = Math.Max(0, finalSize.Height - (SheetBorder.Margin.Top + SheetBorder.Margin.Bottom));

            return base.ArrangeOverride(finalSize);
        }

        internal void ShowSheet(SheetControl control)
        {
            SheetRoot.Child = control;
            control.InvokeShowing();

            VisualStateManager.GoToState(this, "Open", true);

            Window.Current.SetTitleBar(TitleBar);
            var safeAreaService = Ioc.Default.GetRequiredService<ISafeAreaService>();
            safeAreaService.SafeAreaUpdated += OnSafeAreaUpdated;
        }

        internal async Task<bool> HideSheetAsync()
        {
            // TODO: allow deferrals
            if (SheetRoot.Child is SheetControl control)
            {
                if (!await control.InvokeHidingAsync())
                    return false;
            }

            VisualStateManager.GoToState(this, "Closed", true);

            var safeAreaService = Ioc.Default.GetRequiredService<ISafeAreaService>();
            safeAreaService.SafeAreaUpdated -= OnSafeAreaUpdated;

            return true;
        }

        private void OnSafeAreaUpdated(object sender, SafeAreaUpdatedEventArgs e)
        {
            TitleBar.Height = e.SafeArea.Bounds.Top;
            SheetBorder.Margin = new Thickness(0, 16 + e.SafeArea.Bounds.Top, 0, 0);
            SheetRoot.Height = Math.Max(0, ActualHeight - (SheetBorder.Margin.Top + SheetBorder.Margin.Bottom));
            HostControl.Margin = new Thickness(e.SafeArea.Bounds.Left, 0, e.SafeArea.Bounds.Right, e.SafeArea.Bounds.Bottom);
        }

        private async void RefreshContainer_RefreshRequested(MUXC.RefreshContainer sender, MUXC.RefreshRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();
            await HideSheetAsync();
            deferral.Complete();
        }

        private void ShowSheetStoryboard_Completed(object sender, object e)
        {
            if (SheetRoot.Child is SheetControl control)
            {
                control.InvokeShown();
            }

            CommonShadow.CastTo = CompositionBackdropContainer;
            Effects.SetShadow(SheetBorder, CommonShadow);
        }

        private void HideSheetStoryboard_Completed(object sender, object e)
        {
            if (SheetRoot.Child is SheetControl control)
            {
                control.InvokeHidden();
                SheetRoot.Child = null;
            }

            Effects.SetShadow(SheetBorder, null);
        }
    }
}
