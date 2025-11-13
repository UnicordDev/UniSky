using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FishyFlip;
using FishyFlip.Lexicon.Com.Atproto.Server;
using FishyFlip.Models;
using FishyFlip.Tools;
using Microsoft.Extensions.Logging;
using OwlCore.Extensions;
using UniSky.Extensions;
using UniSky.Models;
using UniSky.Services;
using UniSky.ViewModels.Error;
using Windows.ApplicationModel.Store.Preview;
using Windows.Networking.Connectivity;
using Windows.Security.Authentication.Web;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Authentication.Web.Provider;
using Windows.Security.Credentials;
using Windows.UI.ApplicationSettings;

namespace UniSky.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly ILoginService loginService;
    private readonly ISessionService sessionService;
    private readonly IRootNavigator rootNavigator;
    private readonly ILoggerFactory loggerFactory;

    [ObservableProperty]
    private bool _advanced;
    [ObservableProperty]
    private string _username;
    [ObservableProperty]
    private string _password;
    [ObservableProperty]
    private string _host;

    public LoginViewModel(ILoginService loginService,
                          ISessionService sessionService,
                          IRootNavigator rootNavigator,
                          ILoggerFactory loggerFactory)
    {
        this.loginService = loginService;
        this.sessionService = sessionService;
        this.rootNavigator = rootNavigator;
        this.loggerFactory = loggerFactory;

        Advanced = false;
        Username = "";
        Password = "";
        Host = "https://bsky.social";

        var pane = AccountsSettingsPane.GetForCurrentView();
        pane.AccountCommandsRequested += Pane_AccountCommandsRequested;
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

            var builder = new ATProtocolBuilder()
                .EnableAutoRenewSession(true)
                .WithUserAgent(Constants.UserAgent)
                .WithInstanceUrl(new Uri(Host))
                .WithLogger(loggerFactory.CreateLogger("ATProtocol_Login"));

            using var protocol = builder.Build();


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
            var sessionModel = new SessionModel(true, normalisedHost, session);

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
    private async Task ToggleAdvancedAsync()
    {
        //Advanced = !Advanced;


        AccountsSettingsPane.Show();


    }

    private async void Pane_AccountCommandsRequested(AccountsSettingsPane sender, AccountsSettingsPaneCommandsRequestedEventArgs args)
    {
        var deferral = args.GetDeferral();

        //var accounts = await WebAccountManager.FindAllProviderWebAccountsAsync();
        //foreach (WebAccount account in accounts)
        //{
        //    WebAccountCommand command = new WebAccountCommand(account, WebACcountCommandInvoked, SupportedWebAccountActions.Reconnect);
        //    args.WebAccountCommands.Add(command);
        //}

        var oauthProvider = await WebAuthenticationCoreManager.FindAccountProviderAsync("https://atproto.wamwoowam.co.uk/");
        args.WebAccountProviderCommands.Add(new WebAccountProviderCommand(oauthProvider, OnGetBlueskyTokenAsync));

        deferral.Complete();
    }

    private async void WebACcountCommandInvoked(WebAccountCommand command, WebAccountInvokedArgs args)
    {
        WebTokenRequest request = new WebTokenRequest(command.WebAccount.WebAccountProvider);
        WebTokenRequestResult result = await WebAuthenticationCoreManager.RequestTokenAsync(request);
    }

    private async void OnGetBlueskyTokenAsync(WebAccountProviderCommand command)
    {
        await syncContext.PostAsync(async () =>
        {
            try
            {
                WebTokenRequest request = new WebTokenRequest(command.WebAccountProvider, "asdf", "asdf", WebTokenRequestPromptType.ForceAuthentication);
                WebTokenRequestResult result = await WebAuthenticationCoreManager.RequestTokenAsync(request);
            }
            catch (Exception ex)
            {

            }
        });
    }
}
