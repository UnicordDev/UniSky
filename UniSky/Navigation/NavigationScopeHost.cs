using System;
using Microsoft.Extensions.DependencyInjection;
using UniSky.Extensions;
using UniSky.Services;
using UniSky.Services.Navigation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UniSky.Navigation;

/// <summary>
/// Declares navigation scopes from XAML, and resolves the scope an element sits inside.
/// </summary>
public static class NavigationScopeHost
{
    public static readonly DependencyProperty ScopeProperty =
        DependencyProperty.RegisterAttached("Scope", typeof(string), typeof(NavigationScopeHost),
            new PropertyMetadata(null, OnScopeChanged));

    public static readonly DependencyProperty KindProperty =
        DependencyProperty.RegisterAttached("Kind", typeof(NavigationScopeKind), typeof(NavigationScopeHost),
            new PropertyMetadata(NavigationScopeKind.Content));

    public static readonly DependencyProperty PolicyProperty =
        DependencyProperty.RegisterAttached("Policy", typeof(NavigationScopePolicy), typeof(NavigationScopeHost),
            new PropertyMetadata(NavigationScopePolicy.BubbleUnknown));

    private static readonly DependencyProperty ScopeInstanceProperty =
        DependencyProperty.RegisterAttached("ScopeInstance", typeof(INavigationScope), typeof(NavigationScopeHost),
            new PropertyMetadata(null));

    private static readonly DependencyProperty RegistrationProperty =
        DependencyProperty.RegisterAttached("Registration", typeof(IDisposable), typeof(NavigationScopeHost),
            new PropertyMetadata(null));

    public static string GetScope(DependencyObject element)
        => (string)element.GetValue(ScopeProperty);

    public static void SetScope(DependencyObject element, string value)
        => element.SetValue(ScopeProperty, value);

    public static NavigationScopeKind GetKind(DependencyObject element)
        => (NavigationScopeKind)element.GetValue(KindProperty);

    public static void SetKind(DependencyObject element, NavigationScopeKind value)
        => element.SetValue(KindProperty, value);

    public static NavigationScopePolicy GetPolicy(DependencyObject element)
        => (NavigationScopePolicy)element.GetValue(PolicyProperty);

    public static void SetPolicy(DependencyObject element, NavigationScopePolicy value)
        => element.SetValue(PolicyProperty, value);

    /// <summary>
    /// The scope declared on this element, if it has been built yet.
    /// </summary>
    public static INavigationScope GetScopeInstance(DependencyObject element)
        => (INavigationScope)element?.GetValue(ScopeInstanceProperty);

    /// <summary>
    /// The nearest scope at or above <paramref name="element"/>, falling back to this view's shell.
    /// </summary>
    public static INavigationScope FindFor(DependencyObject element)
    {
        var current = element;
        while (current != null)
        {
            if (GetScopeInstance(current) is INavigationScope scope)
                return scope;

            current = VisualTreeHelper.GetParent(current);
        }

        return ServiceContainer.Scoped.GetService<INavigationScopeRegistry>()?.Shell;
    }
    
    public static void Detach(DependencyObject element)
    {
        if (element == null)
            return;

        if (element.GetValue(RegistrationProperty) is IDisposable registration)
            registration.Dispose();

        element.ClearValue(RegistrationProperty);
        element.ClearValue(ScopeInstanceProperty);
    }

    private static void OnScopeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            return;

        Detach(element);

        if (e.NewValue is not string id || id.Length == 0)
            return;

        element.Loaded -= OnElementLoaded;
        element.Loaded += OnElementLoaded;

        if (element.IsLive())
            EnsureScope(element);
    }

    private static void OnElementLoaded(object sender, RoutedEventArgs e)
        => EnsureScope((FrameworkElement)sender);

    /// <summary>
    /// Builds the scope declared on an element now, rather than waiting for it to load.
    /// </summary>
    public static INavigationScope EnsureScope(FrameworkElement element)
    {
        if (GetScopeInstance(element) is INavigationScope existing)
        {
            AttachToParent(element, existing);
            return existing;
        }

        var id = GetScope(element);
        if (string.IsNullOrEmpty(id))
            return null;

        var services = ServiceContainer.Scoped;
        var registry = services.GetRequiredService<INavigationScopeRegistry>();
        var routeTable = services.GetRequiredService<IRouteTable>();
        var backCoordinator = services.GetRequiredService<IBackNavigationCoordinator>();

        var scope = new FrameNavigationScope(
            id,
            GetKind(element),
            GetPolicy(element),
            element as Frame,
            routeTable,
            registry,
            backCoordinator);

        element.SetValue(ScopeInstanceProperty, scope);
        element.SetValue(RegistrationProperty, registry.Register(scope));

        AttachToParent(element, scope);
        return scope;
    }

    private static void AttachToParent(FrameworkElement element, INavigationScope scope)
    {
        if (scope.Parent != null)
            return;

        var parent = VisualTreeHelper.GetParent(element);
        while (parent != null)
        {
            if (GetScopeInstance(parent) is INavigationScope parentScope)
            {
                parentScope.AddChild(scope);
                return;
            }

            parent = VisualTreeHelper.GetParent(parent);
        }
    }
}
