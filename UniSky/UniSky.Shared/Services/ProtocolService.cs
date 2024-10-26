using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using FishyFlip;
using FishyFlip.Events;
using Microsoft.Extensions.Logging;
using UniSky.Models;

namespace UniSky.Services;

internal class ProtocolService(ILogger<ProtocolService> logger) : IProtocolService
{
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

    private void OnSessionUpdated(object sender, SessionUpdatedEventArgs e)
    {
        logger.LogInformation("Session updated, saving new tokens!");

        var session = new SessionModel(true, e.InstanceUri.Host.ToLowerInvariant(), e.Session.Session, e.Session);
        var sessionService = Ioc.Default.GetRequiredService<SessionService>();
        sessionService.SaveSession(session);
    }
}
