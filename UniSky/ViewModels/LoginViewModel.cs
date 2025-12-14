using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.Com.Atproto.Server;
using FishyFlip.Models;
using FishyFlip.Tools;
using Microsoft.Extensions.Logging;
using OwlCore.Extensions;
using UniSky.Extensions;
using UniSky.Messages;
using UniSky.Models;
using UniSky.Services;
using UniSky.ViewModels.Error;
using UniSky.ViewModels.Profile;
using Windows.ApplicationModel.Activation;
using Windows.System;
using Windows.UI.ViewManagement;
using static UniSky.Constants;

namespace UniSky.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly ILoginService loginService;
    private readonly ISessionService sessionService;
    private readonly IRootNavigator rootNavigator;
    private readonly ILoggerFactory loggerFactory;
    private readonly IProtocolService protocolService;

    [ObservableProperty]
    private bool _advanced;
    [ObservableProperty]
    private string _username;
    [ObservableProperty]
    private string _password;
    [ObservableProperty]
    private string _host;

    [ObservableProperty]
    private int _step;
    [ObservableProperty]
    private ProfileViewModel _loginUser;

    private readonly ATProtocol protocol;

    public LoginViewModel(ILoginService loginService,
                          IProtocolService protocolService,
                          ISessionService sessionService,
                          IRootNavigator rootNavigator,
                          ILoggerFactory loggerFactory)
    {
        this.loginService = loginService;
        this.sessionService = sessionService;
        this.rootNavigator = rootNavigator;
        this.loggerFactory = loggerFactory;
        this.protocolService = protocolService;

        Advanced = false;
        Username = "";
        Password = "";
        Host = "https://bsky.social";

        var builder = new ATProtocolBuilder()
            .EnableAutoRenewSession(true)
            .WithUserAgent(Constants.UserAgent)
            .WithLogger(loggerFactory.CreateLogger("ATProtocol_Login"));

        this.protocol = builder.Build();

        WeakReferenceMessenger.Default.Register<ProtocolActivatedMessage>(this,
            (o, e) => ((LoginViewModel)o).OnProtocolActivated(e.EventArgs));
    }

    [RelayCommand]
    private async Task Login()
    {
        Error = null;

        using var context = GetLoadingContext();
        try
        {
            var normalisedHost = new UriBuilder(Host)
                .Host.ToLowerInvariant();


            var createSession = (await protocol.CreateSessionAsync(Username, Password)
                .ConfigureAwait(false))
                .HandleResult();

            var didDoc = createSession.DidDoc;
            if (didDoc == null)
            {
                didDoc = (await protocol.PlcDirectory.GetDidDocAsync(createSession.Did)
                    .ConfigureAwait(false))
                    .HandleResult();
            }

            var session = new Session(createSession.Did,
                                      didDoc,
                                      createSession.Handle,
                                      createSession.Email,
                                      createSession.AccessJwt,
                                      createSession.RefreshJwt,
                                      DateTime.MaxValue);
            var loginModel = this.loginService.SaveLogin(normalisedHost, Username, Password);
            var sessionModel = new SessionModel(true, false, normalisedHost, session);

            sessionService.SaveSession(sessionModel);

            await rootNavigator.GoToHomeAsync(sessionModel.DID);
        }
        catch (Exception ex)
        {
            syncContext.Post(() =>
                 Error = new ExceptionViewModel(ex));
        }
    }

    [RelayCommand]
    private async Task OAuthLogin()
    {
        Error = null;
        using var context = GetLoadingContext();

        try
        {
            var identifer = ATIdentifier.Parse(Username, CultureInfo.InvariantCulture);
            var uri = (await protocol.GenerateOAuth2AuthenticationUrlResultAsync(CLIENT_ID, OAUTH_CALLBACK, ["atproto", "transition:generic"], identifer)
                    .ConfigureAwait(false))
                    .HandleResult();

            await syncContext.PostAsync(async () =>
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
            Step = 0;
            syncContext.Post(() =>
                 Error = new ExceptionViewModel(ex));
        }
    }

    private void OnProtocolActivated(ProtocolActivatedEventArgs e)
    {
        if (e.Uri.Host != "oauth-callback")
        {
            return;
        }

        Task.Run(async () =>
        {
            Step = 2;
            try
            {
                var createSession = (await protocol.AuthenticateWithOAuth2CallbackResultAsync(e.Uri.ToString())
                    .ConfigureAwait(false))
                    .HandleResult();


                protocolService.SetProtocol(protocol);

                var profile = (await protocol.GetProfileAsync(createSession.Did)
                    .ConfigureAwait(false))
                    .HandleResult();

                syncContext.Post(() =>
                {
                    LoginUser = new ProfileViewModel(profile);
                });


                var didDoc = createSession.DidDoc;
                if (didDoc == null)
                {
                    didDoc = (await protocol.GetDidDocAsync(createSession.Did, CancellationToken.None)
                        .ConfigureAwait(false))
                        .HandleResult();
                }

                var normalisedHost = new Uri(didDoc.Service.FirstOrDefault(d => d.Type == "AtprotoPersonalDataServer")?.ServiceEndpoint).Host;
                var session = new Session(createSession.Did,
                                          didDoc,
                                          createSession.Handle,
                                          createSession.Email,
                                          createSession.AccessJwt,
                                          createSession.RefreshJwt,
                                          DateTime.MaxValue);
                //var loginModel = this.loginService.SaveLogin(normalisedHost, Username, Password);
                var sessionModel = new SessionModel(true, true, normalisedHost, session, protocol.AuthSession);

                sessionService.SaveSession(sessionModel);

                await rootNavigator.GoToHomeAsync(sessionModel.DID);
            }
            catch (Exception ex)
            {
                Step = 0;
                syncContext.Post(() =>
                     Error = new ExceptionViewModel(ex));
            }
        });
    }
}
