using System;
using System.Collections.Generic;
using UniSky.Services.Navigation;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace UniSky.Navigation;

/// <summary>
/// A node in the navigation tree, optionally owning a <see cref="Frame"/>.
/// </summary>
internal sealed class FrameNavigationScope : INavigationScope, INavigationNode<NavigationRequest>
{
    private readonly Frame _frame;
    private readonly IRouteTable _routeTable;
    private readonly INavigationScopeRegistry _registry;
    private readonly IBackNavigationCoordinator _backCoordinator;
    private readonly List<INavigationScope> _children = [];

    private NavigationRoute _currentRoute;

    public FrameNavigationScope(
        string id,
        NavigationScopeKind kind,
        NavigationScopePolicy policy,
        Frame frame,
        IRouteTable routeTable,
        INavigationScopeRegistry registry,
        IBackNavigationCoordinator backCoordinator)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Kind = kind;
        Policy = policy;

        _frame = frame;
        _routeTable = routeTable;
        _registry = registry;
        _backCoordinator = backCoordinator;
        
        if (_frame != null)
            _frame.Navigated += OnFrameNavigated;
    }

    private void OnFrameNavigated(object sender, NavigationEventArgs e)
        => _backCoordinator?.Invalidate();

    public string Id { get; }

    public NavigationScopeKind Kind { get; }

    public NavigationScopePolicy Policy { get; }

    public INavigationScope Parent { get; private set; }

    public IReadOnlyList<INavigationScope> Children => _children;

    public INavigationScope ActiveChild { get; set; }

    public Frame Frame => _frame;

    public event TypedEventHandler<INavigationScope, NavigationRequestedEventArgs> Requesting;

    public event TypedEventHandler<INavigationScope, NavigationCompletedEventArgs> Navigated;

    public bool CanGoBack
        => _frame?.CanGoBack ?? ActiveChild?.CanGoBack ?? false;

    public bool GoBack()
    {
        if (_frame is { CanGoBack: true })
        {
            _frame.GoBack();
            return true;
        }

        return ActiveChild?.GoBack() ?? false;
    }

    public bool Supports(NavigationIntent intent)
        => intent is NavigationIntent.Default or NavigationIntent.Replace or NavigationIntent.Root
            && _frame != null
            && Kind != NavigationScopeKind.Shell;

    public bool Navigate(NavigationRequest request)
    {
        if (request == null)
            return false;

        if (!string.IsNullOrEmpty(request.TargetScope) && request.TargetScope != Id)
        {
            var target = _registry.Find(request.TargetScope);
            if (target != null)
                return target.Handle(request);
        }

        return NavigationWalk.Navigate(this, request);
    }

    INavigationNode<NavigationRequest> INavigationNode<NavigationRequest>.NodeParent
        => Parent as INavigationNode<NavigationRequest>;

    INavigationNode<NavigationRequest> INavigationNode<NavigationRequest>.NodeActiveChild
        => ActiveChild as INavigationNode<NavigationRequest>;

    /// <summary>
    /// Raises <see cref="Requesting"/>, letting a host claim, veto or rewrite the request.
    /// </summary>
    public bool TryIntercept(ref NavigationRequest request, out bool result)
    {
        var handler = Requesting;
        if (handler == null)
        {
            result = false;
            return true;
        }

        var args = new NavigationRequestedEventArgs(request);
        handler(this, args);

        if (args.Cancel)
        {
            result = false;
            return false;
        }

        if (args.Handled)
        {
            result = true;
            return false;
        }

        request = args.Request ?? request;
        result = false;
        return true;
    }

    public bool CanHandleHere(NavigationRequest request)
        => Supports(request.Intent) && _routeTable.TryResolve(request.Route, out _);

    public bool Handle(NavigationRequest request)
    {
        if (request == null || _frame == null || !_routeTable.TryResolve(request.Route, out var pageType))
            return false;

        if (request.Intent == NavigationIntent.Default
            && _frame.Content?.GetType() == pageType
            && request.Route.Equals(_currentRoute))
        {
            return false;
        }

        if (request.Intent == NavigationIntent.Root)
            _frame.BackStack.Clear();

        
        // at this point we _are_ navigating. so connected animations get kicked off here
        var animation = ConnectedAnimations.IsEnabled ? request.Animation : null;
        ConnectedAnimations.Prepare(animation);
        var transition = request.Transition
            ?? (animation != null ? new SuppressNavigationTransitionInfo() : null);

        var success = _frame.Navigate(pageType, request, transition);
        if (!success)
            ConnectedAnimations.Cancel(animation?.Key);

        if (success)
        {
            _currentRoute = request.Route;

            if (request.Intent == NavigationIntent.Replace && _frame.BackStack.Count > 0)
                _frame.BackStack.RemoveAt(_frame.BackStack.Count - 1);
        }

        Navigated?.Invoke(this, new NavigationCompletedEventArgs(request, success));
        return success;
    }

    public void AddChild(INavigationScope child)
    {
        if (child == null || _children.Contains(child))
            return;

        _children.Add(child);

        if (child is FrameNavigationScope scope)
            scope.Parent = this;

        ActiveChild ??= child;
    }

    public void RemoveChild(INavigationScope child)
    {
        if (child == null || !_children.Remove(child))
            return;

        if (child is FrameNavigationScope scope && ReferenceEquals(scope.Parent, this))
            scope.Parent = null;

        if (ReferenceEquals(ActiveChild, child))
            ActiveChild = _children.Count > 0 ? _children[_children.Count - 1] : null;
    }

    public IReadOnlyList<string> GetSerializedState()
    {
        if (_frame == null)
            return ActiveChild?.GetSerializedState() ?? [];

        var state = new List<string>(_frame.BackStackDepth + 1);
        foreach (var entry in _frame.BackStack)
        {
            if (entry.Parameter is NavigationRequest request)
                state.Add(request.Route.ToCanonicalString());
        }

        if (_currentRoute != null)
            state.Add(_currentRoute.ToCanonicalString());

        return state;
    }

    public override string ToString()
        => $"{Id} ({Kind}, {Policy}, {(_frame != null ? "framed" : "frameless")})";
}
