using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.Com.Atproto.Sync;
using FishyFlip.Models;
using Microsoft.Extensions.Logging;
using UniSky.Extensions;
using UniSky.Messages;
using UniSky.Services;
using UniSky.ViewModels;
using UniSky.ViewModels.Profile;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Networking.NetworkOperators;
using Windows.Security.Authentication.Web;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Authentication.Web.Provider;
using Windows.Security.Credentials;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.ViewManagement;

using AccountManager = Windows.Security.Authentication.Web.Provider.WebAccountManager;

namespace UniSky.Areas.WebAccountManager.ViewModels;

public partial class AccountManagerAddAccountViewModel : ViewModelBase
{
    private const string CLIENT_ID = "https://u.hasthebig.gay/~wam/unisky/client-metadata.json";
    private const string OAUTH_CALLBACK = "https://u.hasthebig.gay/~wam/unisky/oauth-callback.html";

    private readonly IProtocolService protocolService;
    private readonly WebAccountProviderActivatedEventArgs eventArgs;
    private readonly ATProtocol protocol;

    [ObservableProperty]
    private string _handle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStep1))]
    [NotifyPropertyChangedFor(nameof(IsStep2))]
    [NotifyPropertyChangedFor(nameof(IsStep3))]
    private int _step;

    [ObservableProperty]
    private ProfileViewModel _loginUser;

    public AccountManagerAddAccountViewModel(IProtocolService protocolService, ILoggerFactory loggerFactory,
                                             WebAccountProviderActivatedEventArgs eventArgs)
    {
        this.eventArgs = eventArgs;
        this.protocolService = protocolService;

        var builder = new ATProtocolBuilder()
            .EnableAutoRenewSession(true)
            .WithUserAgent(Constants.UserAgent)
            .WithLogger(loggerFactory.CreateLogger("ATProtocol_Login"));

        this.protocol = builder.Build();

        var args = (WebAccountProviderRequestTokenOperation)eventArgs.Operation;
        var request = args.ProviderRequest;

        WeakReferenceMessenger.Default.Register<ProtocolActivatedMessage>(this,
            (o, e) => ((AccountManagerAddAccountViewModel)o).OnProtocolActivated(e.EventArgs));
    }


    public bool IsStep1 => Step == 0;
    public bool IsStep2 => Step == 1;
    public bool IsStep3 => Step == 2;

    public async Task BeginLoginAsync()
    {
        var args = (WebAccountProviderRequestTokenOperation)eventArgs.Operation;
        var request = args.ProviderRequest;

        using var context = this.GetLoadingContext();
        try
        {
            var identifer = ATIdentifier.Parse(Handle, CultureInfo.InvariantCulture);

            var uri = (await protocol.GenerateOAuth2AuthenticationUrlResultAsync(CLIENT_ID, OAUTH_CALLBACK, ["atproto", "transition:generic"], identifer)
                .ConfigureAwait(false))
                .HandleResult();

            syncContext.Post(async () =>
            {
                var options = new LauncherOptions()
                {
                    DesiredRemainingView = ViewSizePreference.UseMinimum
                };

                await Launcher.LaunchUriAsync(new Uri(uri), options);
            });

            Step = 1;
        }
        catch (Exception ex)
        {
            SetErrored(ex);
        }
    }

    private async void OnProtocolActivated(ProtocolActivatedEventArgs e)
    {
        var args = (WebAccountProviderRequestTokenOperation)eventArgs.Operation;
        var request = args.ProviderRequest;

        if (e.Uri.Host == "oauth-callback")
        {
            Step = 2;

            var session = (await protocol.AuthenticateWithOAuth2CallbackResultAsync(e.Uri.ToString())
                .ConfigureAwait(false))
                .HandleResult();

            protocolService.SetProtocol(protocol);

            var profile = (await protocol.GetProfileAsync(session.Did)
                .ConfigureAwait(false))
                .HandleResult();

            syncContext.Post(() =>
            {
                LoginUser = new ProfileViewModel(profile);
            });


            try
            {
                var props = new Dictionary<string, string>(request.ClientRequest.Properties);
                if (!string.IsNullOrEmpty(session.Email))
                    props["Email"] = session.Email;

                var account = await AccountManager.AddWebAccountAsync(session.Did.ToString().Replace(':', '_'), profile.DisplayName ?? profile.Handle.ToString(), props);
                //if (profile.Avatar != null)
                //{
                //    var streamRef = RandomAccessStreamReference.CreateFromUri(new Uri(profile.Avatar));
                //    using var stream = await streamRef.OpenReadAsync();

                //    await AccountManager.SetWebAccountPictureAsync(account, stream);
                //}

                var tokenResponse = new WebTokenResponse(session.AccessJwt, account);
                var providerTokenResponse = new WebProviderTokenResponse(tokenResponse);
                args.ProviderResponses.Add(providerTokenResponse);
                args.CacheExpirationTime = session.ExpiresIn;
                args.ReportCompleted();
            }
            catch (Exception ex)
            {

            }

        }
    }
}
