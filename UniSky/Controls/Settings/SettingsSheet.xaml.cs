using System;
using Microsoft.Extensions.DependencyInjection;
using UniSky.Controls.Overlay;
using UniSky.Controls.Sheet;
using UniSky.Services;
using UniSky.ViewModels.Settings;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Controls;

namespace UniSky.Controls.Settings;

public sealed partial class SettingsSheet : SheetControl
{
    public SettingsSheet()
    {
        this.InitializeComponent();
        this.Showing += OnShowing;
        this.Hiding += OnHiding;
    }

    private void OnShowing(IOverlayControl sender, OverlayShowingEventArgs args)
    {
        this.DataContext = ActivatorUtilities.CreateInstance<SettingsViewModel>(ServiceContainer.Scoped);
    }

    private async void OnHiding(IOverlayControl sender, OverlayHidingEventArgs args)
    {
        if (!ApiInformation.IsMethodPresent("Windows.ApplicationModel.Core.CoreApplication", "RequestRestartAsync"))
            return;

        if (this.DataContext is not SettingsViewModel { IsDirty: true })
            return;

        var deferral = args.GetDeferral();
        try
        {
            var needsRelaunchDialog = new SettingsNeedsRelaunchDialog();
            if (Controller != null && ApiInformation.IsApiContractPresent(typeof(UniversalApiContract).FullName, 8))
                needsRelaunchDialog.XamlRoot = Controller.Root.XamlRoot;

            if (await needsRelaunchDialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await CoreApplication.RequestRestartAsync("");
            }
        }
        finally
        {
            deferral.Complete();
        }
    }
}
