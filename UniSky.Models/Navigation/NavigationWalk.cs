namespace UniSky.Services.Navigation;

/// <summary>
/// Routes a navigation request through a scope tree.
/// </summary>
public static class NavigationWalk
{
    /// <summary>
    /// Walks a request outwards from <paramref name="origin"/> until a node claims it.
    /// </summary>
    public static bool Navigate<TRequest>(INavigationNode<TRequest> origin, TRequest request)
    {
        INavigationNode<TRequest> declined = null;
        var current = origin;

        while (current != null)
        {
            if (!current.TryIntercept(ref request, out var intercepted))
                return intercepted;

            if (current.CanHandleHere(request))
                return current.Handle(request);

            var child = current.NodeActiveChild;
            if (child != null && !ReferenceEquals(child, declined) && NavigateDown(child, request))
                return true;

            if (current.Policy == NavigationScopePolicy.Contain)
                return false;

            declined = current;
            current = current.NodeParent;
        }

        return false;
    }

    /// <summary>
    /// Tries a node and its active descendants, never travelling back up.
    /// </summary>
    private static bool NavigateDown<TRequest>(INavigationNode<TRequest> node, TRequest request)
    {
        while (node != null)
        {
            if (!node.TryIntercept(ref request, out var intercepted))
                return intercepted;

            if (node.CanHandleHere(request))
                return node.Handle(request);

            node = node.NodeActiveChild;
        }

        return false;
    }
}
