using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using UniSky.Services.Navigation;

namespace UniSky.Navigation;

internal sealed class ActivationCoordinator(ILogger<ActivationCoordinator> logger) : IActivationCoordinator
{
    private readonly Queue<NavigationRequest> _pending = new();

    private INavigationContext _target;

    public void Enqueue(NavigationRequest request)
    {
        if (request == null)
            return;

        _pending.Enqueue(request);
        Flush();
    }

    public void SetTarget(INavigationContext target)
    {
        _target = target;
        Flush();
    }

    private void Flush()
    {
        if (_target == null)
            return;

        while (_pending.Count > 0)
        {
            var request = _pending.Dequeue();
            if (!_target.Navigate(request))
                logger.LogWarning("Nothing handled activation request {Request}", request);
        }
    }
}
