using System;
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

namespace UniSky.Services;

public class ProtocolService(ILogger<ProtocolService> logger) : IProtocolService
{
    private readonly SemaphoreSlim refreshTokenSemaphore = new SemaphoreSlim(1, 1);

    private ATProtocol _protocol = null;

    public ATProtocol Protocol
        => _protocol ?? throw new InvalidOperationException("Protocol not yet initialized.");

    public void SetProtocol(ATProtocol protocol)
    {
        if (_protocol != null)
        {
            _protocol.SessionUpdated -= OnSessionUpdated;
        }

        protocol.SessionUpdated += OnSessionUpdated;
        _protocol = protocol;
    }

    public async Task RefreshSessionAsync(SessionModel sessionModel)
    {
        var protocol = Protocol;

        // make sure we're not doing this multiple times
        if (!await refreshTokenSemaphore.WaitAsync(100))
            return;

        try
        {
            // to ensure the session gets refreshed properly:
            // - initially authenticate the client with the refresh token
            // - refresh the sesssion
            // - reauthenticate with the new session

            var sessionRefresh = sessionModel.Session.Session;
            var authSessionRefresh = new AuthSession(
                new Session(sessionRefresh.Did, sessionRefresh.DidDoc, sessionRefresh.Handle, null, sessionRefresh.RefreshJwt, sessionRefresh.RefreshJwt));

            await protocol.AuthenticateWithPasswordSessionAsync(authSessionRefresh);
            var refreshSession = (await protocol.RefreshSessionAsync()
                .ConfigureAwait(false))
                .HandleResult();

            var authSession2 = new AuthSession(
                    new Session(refreshSession.Did, refreshSession.DidDoc, refreshSession.Handle, null, refreshSession.AccessJwt, refreshSession.RefreshJwt));
            var session2 = await protocol.AuthenticateWithPasswordSessionAsync(authSession2)
                .ConfigureAwait(false);

            var moderationService = ServiceContainer.Default.GetService<IModerationService>();
            if (moderationService != null)
            {
                await protocol.ConfigureLabelersAsync(moderationService.ModerationOptions?.Prefs?.Labelers ?? []);
            }

            if (session2 == null)
                throw new InvalidOperationException("Authentication failed!");

            logger.LogInformation("Successfully refreshed session.");

            var sessionModel2 = new SessionModel(true, sessionModel.Service, authSession2.Session, authSession2);
            var sessionService = ServiceContainer.Scoped.GetRequiredService<ISessionService>();
            sessionService.SaveSession(sessionModel2);

            SetProtocol(protocol);
        }
        finally
        {
            refreshTokenSemaphore.Release();
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
