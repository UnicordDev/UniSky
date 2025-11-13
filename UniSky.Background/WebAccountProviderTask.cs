using System;
using System.ComponentModel.DataAnnotations;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Notification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UniSky.Services;
using Windows.ApplicationModel.Background;
using Windows.Security.Authentication.Web.Provider;

namespace UniSky.Background;

public sealed class WebAccountProviderTask : IBackgroundTask
{
    public WebAccountProviderTask()
    {
    }

    public async void Run(IBackgroundTaskInstance taskInstance)
    {
        var deferral = taskInstance.GetDeferral();
        var logger = ServiceContainer.Default.GetRequiredService<ILogger<WebAccountProviderTask>>();
        try
        {
            var trigger = (WebAccountProviderTriggerDetails)taskInstance.TriggerDetails;
            switch (trigger.Operation)
            {
                case WebAccountProviderDeleteAccountOperation deleteOperation:
                    {
                        await WebAccountManager.DeleteWebAccountAsync(deleteOperation.WebAccount);
                        deleteOperation.ReportCompleted();
                        break;
                    }
                case WebAccountProviderGetTokenSilentOperation getTokenSilentOperation:
                    {
                        getTokenSilentOperation.ReportUserInteractionRequired();
                        break;
                    }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to run {nameof(WebAccountProviderTask)}");
        }
        finally
        {
            deferral.Complete();
        }
    }
}
