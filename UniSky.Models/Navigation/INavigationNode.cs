namespace UniSky.Services.Navigation;

/// <summary>
/// The bare shape <see cref="NavigationWalk"/> needs to route a request through a scope tree.
/// </summary>
public interface INavigationNode<TRequest>
{
    INavigationNode<TRequest> NodeParent { get; }

    INavigationNode<TRequest> NodeActiveChild { get; }

    NavigationScopePolicy Policy { get; }

    /// <summary>
    /// Gives a host attached to this node the chance to claim, veto or rewrite the request.
    /// </summary>
    bool TryIntercept(ref TRequest request, out bool result);

    /// <summary>
    /// Whether this node could perform the request itself.
    /// </summary>
    bool CanHandleHere(TRequest request);

    /// <summary>
    /// Performs the request here, without further traversal.
    /// </summary>
    bool Handle(TRequest request);
}
