using System;
using System.Linq;
using System.Net.Http.Headers;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using FishyFlip;
using FishyFlip.Events;
using FishyFlip.Lexicon.Com.Atproto.Server;
using FishyFlip.Models;
using FishyFlip.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UniSky.Models;
using UniSky.Moderation;
using Windows.System.UserProfile;

namespace UniSky.Services;

public class ProtocolService(ILogger<ProtocolService> logger) : IProtocolService
{
    private readonly SemaphoreSlim refreshTokenSemaphore = new SemaphoreSlim(1, 1);

    private ATProtocol _protocol = null;
    private DateTimeOffset _lastRefreshed;

    public ATProtocol Protocol
        => _protocol ?? throw new InvalidOperationException("Protocol not yet initialized.");

    public void SetProtocol(ATProtocol protocol)
    {
        if (_protocol != null)
        {
            _protocol.SessionUpdated -= OnSessionUpdated;
        }

        protocol.SessionUpdated += OnSessionUpdated;

        protocol.Client.DefaultRequestHeaders.AcceptLanguage.Clear();
        foreach (var item in GlobalizationPreferences.Languages)
        {
            var idx = item.IndexOf('-');
            if (idx > 0)
            {
                var langCode = item.Substring(0, idx);
                protocol.Client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(langCode));
            }

            protocol.Client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(item));
        }

        _protocol = protocol;
    }

    public async Task RefreshSessionAsync(SessionModel sessionModel)
    {
        //var protocol = Protocol;

        // make sure we're not doing this multiple times
        await refreshTokenSemaphore.WaitAsync();

        try
        {
            // dont refresh more than once per hour
            // TODO: could maybe use the session expiry date passed but that seems a little bit prone to failure
            if ((DateTimeOffset.Now - _lastRefreshed) < TimeSpan.FromHours(1))
                return;

            _lastRefreshed = DateTimeOffset.Now;

            await DoRefreshAsync(logger, sessionModel)
                .ConfigureAwait(false);
        }
        finally
        {
            refreshTokenSemaphore.Release();
        }
    }

    private async Task DoRefreshAsync(ILogger<ProtocolService> logger, SessionModel sessionModel)
    {
        // to ensure the session gets refreshed properly:
        // - initially authenticate the client with the refresh token
        // - refresh the sesssion
        // - reauthenticate with the new session
        var temporaryProtocol = new ATProtocolBuilder(Protocol.Options)
            .Build();

        var sessionRefresh = sessionModel.Session.Session;
        var refreshSession = new AuthSession(
            new Session(sessionRefresh.Did,
                        sessionRefresh.DidDoc,
                        sessionRefresh.Handle,
                        sessionRefresh.Email,
                        sessionRefresh.RefreshJwt,
                        sessionRefresh.RefreshJwt,
                        sessionRefresh.ExpiresIn));

        await temporaryProtocol.AuthenticateWithPasswordSessionResultAsync(refreshSession)
            .ConfigureAwait(false);

        var refreshedSession = (await temporaryProtocol.RefreshSessionAsync()
            .ConfigureAwait(false))
            .HandleResult();

        var authSession2 = new AuthSession(
                new Session(refreshedSession.Did,
                            refreshedSession.DidDoc ?? sessionRefresh.DidDoc,
                            refreshedSession.Handle,
                            sessionRefresh.Email,
                            refreshedSession.AccessJwt,
                            refreshedSession.RefreshJwt,
                            DateTime.Now + TimeSpan.FromHours(2)));

        var session2 = await temporaryProtocol.AuthenticateWithPasswordSessionResultAsync(authSession2)
            .ConfigureAwait(false);
        if (session2 == null)
            throw new InvalidOperationException("Authentication failed!");

        logger.LogInformation("Successfully refreshed session.");

        var sessionModel2 = new SessionModel(true, sessionModel.Service, authSession2.Session, authSession2);
        var sessionService = ServiceContainer.Scoped.GetRequiredService<ISessionService>();
        sessionService.SaveSession(sessionModel2);

        SetProtocol(temporaryProtocol);

        var moderationService = ServiceContainer.Default.GetService<IModerationService>();
        if (moderationService != null)
        {
            await moderationService.ConfigureModerationAsync()
                .ConfigureAwait(false);
        }
    }

    private void OnSessionUpdated(object sender, SessionUpdatedEventArgs e)
    {
        logger.LogInformation("Session updated, saving new tokens!");

        var session = new SessionModel(true, e.InstanceUri.Host.ToLowerInvariant(), e.Session.Session, e.Session);
        var sessionService = ServiceContainer.Scoped.GetRequiredService<ISessionService>();
        sessionService.SaveSession(session);
    }
}
