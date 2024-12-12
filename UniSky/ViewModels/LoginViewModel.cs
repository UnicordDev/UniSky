using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FishyFlip;
using FishyFlip.Lexicon.Com.Atproto.Server;
using FishyFlip.Models;
using FishyFlip.Tools;
using UniSky.Extensions;
using UniSky.Models;
using UniSky.Pages;
using UniSky.Services;
using UniSky.ViewModels.Error;

namespace UniSky.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly LoginService loginService;
    private readonly SessionService sessionService;
    private readonly INavigationService navigationService;

    [ObservableProperty]
    private bool _advanced;
    [ObservableProperty]
    private string _username;
    [ObservableProperty]
    private string _password;
    [ObservableProperty]
    private string _host;

    public LoginViewModel(LoginService loginService, SessionService sessionService, INavigationServiceLocator navigationServiceLocator)
    {
        this.loginService = loginService;
        this.sessionService = sessionService;
        this.navigationService = navigationServiceLocator.GetNavigationService("Root");

        Advanced = false;
        Username = "";
        Password = "";
        Host = "https://bsky.social";
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
                .WithInstanceUrl(new Uri(Host));

            using var protocol = builder.Build();

            var createSession = (await protocol.CreateSessionAsync(Username, Password, cancellationToken: CancellationToken.None)
                .ConfigureAwait(false))
                .HandleResult();

            var session = new Session(createSession.Did, createSession.DidDoc, createSession.Handle, createSession.Email, createSession.AccessJwt, createSession.RefreshJwt);
            var loginModel = this.loginService.SaveLogin(normalisedHost, Username, Password);
            var sessionModel = new SessionModel(true, normalisedHost, session);

            sessionService.SaveSession(sessionModel);
            syncContext.Post(() =>
                navigationService.Navigate<HomePage>(session.Did));
        }
        catch (Exception ex)
        {
            syncContext.Post(() =>
                 Error = new ExceptionViewModel(ex));
        }
    }

    [RelayCommand]
    private void ToggleAdvanced()
    {
        Advanced = !Advanced;
    }
}
