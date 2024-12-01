using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using UniSky.Controls.Sheet;
using UniSky.ViewModels.Compose;
using UniSky.ViewModels.Posts;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UniSky.Controls.Compose
{
    public sealed partial class ComposeSheet : SheetControl
    {
        public ComposeViewModel ViewModel
        {
            get => (ComposeViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(ComposeViewModel), typeof(ComposeSheet), new PropertyMetadata(null));

        public ComposeSheet()
             : base()
        {
            this.InitializeComponent();
            this.Showing += OnShowing;
            this.Shown += OnShown;
            this.Hiding += OnHiding;
        }

        private void OnShowing(SheetControl sender, SheetShowingEventArgs e)
        {
            var inputPane = InputPane.GetForCurrentView();
            inputPane.Showing += OnInputPaneShowing;
            inputPane.Hiding += OnInputPaneHiding;

            if (e.Parameter is PostViewModel replyTo)
            {
                this.ViewModel = ActivatorUtilities.CreateInstance<ComposeViewModel>(Ioc.Default, replyTo);
            }
            else
            {
                this.ViewModel = ActivatorUtilities.CreateInstance<ComposeViewModel>(Ioc.Default);
            }
        }

        private void OnShown(SheetControl sender, RoutedEventArgs args)
        {
            PrimaryTextBox.Focus(FocusState.Programmatic);
        }

        private async void OnHiding(SheetControl sender, SheetHidingEventArgs e)
        {
            var deferral = e.GetDeferral();
            try
            {
                if (ViewModel.IsDirty && await new ComposeDiscardDraftDialog().ShowAsync() != ContentDialogResult.Primary)
                {
                    e.Cancel = true;
                    return;
                }

                var inputPane = InputPane.GetForCurrentView();
                inputPane.Showing -= OnInputPaneShowing;
                inputPane.Hiding -= OnInputPaneHiding;
            }
            finally
            {
                deferral.Complete();
            }
        }

        private void OnInputPaneShowing(InputPane sender, InputPaneVisibilityEventArgs args)
        {
            if (ActualWidth > 620) return;

            ContentGrid.Padding = new Thickness(0, 0, 0, args.OccludedRect.Height);
            args.EnsuredFocusedElementInView = true;
        }

        private void OnInputPaneHiding(InputPane sender, InputPaneVisibilityEventArgs args)
        {
            if (ActualWidth > 620) return;

            ContentGrid.Padding = new Thickness(0, 0, 0, args.OccludedRect.Height);
            args.EnsuredFocusedElementInView = true;
        }

        public bool Not(bool b, bool a)
            => !a && !b;
    }
}
