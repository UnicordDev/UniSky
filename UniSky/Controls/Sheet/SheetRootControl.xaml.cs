using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI;
using UniSky.Services;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using MUXC = Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UniSky.Controls.Sheet
{
    [ContentProperty(Name = nameof(ContentElement))]
    public sealed partial class SheetRootControl : UserControl
    {
        public FrameworkElement ContentElement
        {
            get => (FrameworkElement)GetValue(ContentElementProperty);
            set => SetValue(ContentElementProperty, value);
        }

        public static readonly DependencyProperty ContentElementProperty =
            DependencyProperty.Register("ContentElement", typeof(FrameworkElement), typeof(SheetRootControl), new PropertyMetadata(null));

        public double TotalHeight
        {
            get => (double)GetValue(TotalHeightProperty);
            set => SetValue(TotalHeightProperty, value);
        }

        // Using a DependencyProperty as the backing store for TotalHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TotalHeightProperty =
            DependencyProperty.Register("TotalHeight", typeof(double), typeof(SheetRootControl), new PropertyMetadata(0.0));

        private SemaphoreSlim _hideSemaphore = new SemaphoreSlim(1, 1);

        public SheetRootControl()
        {
            this.InitializeComponent();
            VisualStateManager.GoToState(this, "Closed", false);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (!double.IsInfinity(HostControl.MaxHeight))
            {
                TotalHeight = 64;
                SheetRoot.Height = Math.Max(0, HostControl.MaxHeight - (SheetBorder.Margin.Top + SheetBorder.Margin.Bottom) - (HostControl.Margin.Top + HostControl.Margin.Bottom));
            }
            else
            {
                TotalHeight = finalSize.Height;
                SheetRoot.Height = Math.Max(0, finalSize.Height - (SheetBorder.Margin.Top + SheetBorder.Margin.Bottom) - (HostControl.Margin.Top + HostControl.Margin.Bottom));
            }

            return base.ArrangeOverride(finalSize);
        }

        internal void ShowSheet(ISheetControl control, object parameter)
        {
            SheetRoot.Child = (FrameworkElement)control;
            control.InvokeShowing(parameter);

            VisualStateManager.GoToState(this, "Open", true);

            var safeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();
            safeAreaService.SafeAreaUpdated += OnSafeAreaUpdated;

            var systemNavigationManager = SystemNavigationManager.GetForCurrentView();
            systemNavigationManager.BackRequested += OnBackRequested;
        }

        private async void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            e.Handled = true;
            await HideSheetAsync();
        }

        internal async Task<bool> HideSheetAsync()
        {
            if (!await _hideSemaphore.WaitAsync(100))
                return false;

            try
            {
                if (SheetRoot.Child is ISheetControl control)
                {
                    if (!await control.InvokeHidingAsync())
                        return false;
                }

                VisualStateManager.GoToState(this, "Closed", true);

                var safeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();
                safeAreaService.SafeAreaUpdated -= OnSafeAreaUpdated;

                var systemNavigationManager = SystemNavigationManager.GetForCurrentView();
                systemNavigationManager.BackRequested -= OnBackRequested;

                return true;
            }
            finally
            {
                _hideSemaphore.Release();
            }
        }

        private void OnSafeAreaUpdated(object sender, SafeAreaUpdatedEventArgs e)
        {
            TitleBar.Height = e.SafeArea.Bounds.Top;
            SheetBorder.Margin = new Thickness(0, 16 + e.SafeArea.Bounds.Top, 0, 0);
            HostControl.Margin = new Thickness(e.SafeArea.Bounds.Left, 0, e.SafeArea.Bounds.Right, e.SafeArea.Bounds.Bottom);

            if (!double.IsInfinity(HostControl.MaxHeight))
            {
                SheetRoot.Height = Math.Max(0, HostControl.MaxHeight - (SheetBorder.Margin.Top + SheetBorder.Margin.Bottom) - (HostControl.Margin.Top + HostControl.Margin.Bottom));
            }
            else
            {
                SheetRoot.Height = Math.Max(0, ActualHeight - (SheetBorder.Margin.Top + SheetBorder.Margin.Bottom) - (HostControl.Margin.Top + HostControl.Margin.Bottom));
            }
        }

        private async void RefreshContainer_RefreshRequested(MUXC.RefreshContainer sender, MUXC.RefreshRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();
            await HideSheetAsync();
            deferral.Complete();
        }

        private void ShowSheetStoryboard_Completed(object sender, object e)
        {
            if (SheetRoot.Child is ISheetControl control)
            {
                control.InvokeShown();
            }

            CommonShadow.CastTo = CompositionBackdropContainer;
            Effects.SetShadow(SheetBorder, CommonShadow);
        }

        private void HideSheetStoryboard_Completed(object sender, object e)
        {
            if (SheetRoot.Child is ISheetControl control)
            {
                control.InvokeHidden();
                SheetRoot.Child = null;
            }

            Effects.SetShadow(SheetBorder, null);
        }
    }
}
