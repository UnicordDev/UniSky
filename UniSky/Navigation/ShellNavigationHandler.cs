using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UniSky.Controls.Sheet;
using UniSky.Services;
using UniSky.Services.Navigation;

namespace UniSky.Navigation;

internal sealed class ShellNavigationHandler
{
    private readonly INavigationScope _shell;
    private readonly ILogger<ShellNavigationHandler> _logger;

    public ShellNavigationHandler(INavigationScope shell, ILogger<ShellNavigationHandler> logger)
    {
        _shell = shell;
        _logger = logger;

        _shell.Requesting += OnRequesting;
    }

    private void OnRequesting(INavigationScope sender, NavigationRequestedEventArgs args)
    {
        if (args.Request.Intent != NavigationIntent.Sheet)
            return;

        args.Handled = true;
        _ = ShowSheetAsync(args.Request);
    }

    private async Task ShowSheetAsync(NavigationRequest request)
    {
        try
        {
            var sheets = ServiceContainer.Scoped.GetRequiredService<ISheetService>();
            await sheets.ShowAsync<NavigationSheet>(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show a navigation sheet for {Request}", request);
        }
    }
}
