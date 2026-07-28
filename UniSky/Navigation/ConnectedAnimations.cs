using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using UniSky.Services;
using UniSky.Services.Navigation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace UniSky.Navigation;

public enum ConnectedAnimationDirection
{
    Forward,
    Back
}

/// <summary>
/// The one place that talks to <see cref="ConnectedAnimationService"/>.
/// </summary>
public static class ConnectedAnimations
{
    public const string ProfileAvatar = "profile.avatar";
    public const string ThreadPost = "thread.post";

    public static bool IsEnabled
        => ServiceContainer.Scoped.GetService<ITypedSettings>()?.EnableConnectedAnimations ?? false;

    public static void Prepare(ConnectedAnimationRequest request)
    {
        if (request == null || !IsEnabled)
            return;

        ConnectedAnimationService.GetForCurrentView()
            .PrepareToAnimate(request.Key, request.Source);
    }
    
    public static void Prepare(string key, UIElement source)
    {
        if (string.IsNullOrEmpty(key) || source == null || !IsEnabled)
            return;

        ConnectedAnimationService.GetForCurrentView()
            .PrepareToAnimate(key, source);
    }
    
    public static void PrepareFromList(string key, ListViewBase list, object item, string elementName)
    {
        if (string.IsNullOrEmpty(key) || list == null || item == null || string.IsNullOrEmpty(elementName) || !IsEnabled)
            return;

        list.PrepareConnectedAnimation(key, item, elementName);
    }
    
    public static void Cancel(string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        ConnectedAnimationService.GetForCurrentView()
            .GetAnimation(key)
            ?.Cancel();
    }
    
    public static bool IsPending(string key)
        => !string.IsNullOrEmpty(key)
        && ConnectedAnimationService.GetForCurrentView().GetAnimation(key) != null;

    public static ConnectedAnimation Get(string key)
        => string.IsNullOrEmpty(key)
            ? null
            : ConnectedAnimationService.GetForCurrentView().GetAnimation(key);
            
    public static bool TryStart(
        string key,
        UIElement destination,
        ConnectedAnimationDirection direction = ConnectedAnimationDirection.Forward,
        params UIElement[] coordinated)
    {
        if (destination == null)
            return false;

        var animation = Get(key);
        if (animation == null)
            return false;

        Configure(animation, direction);

        return coordinated is { Length: > 0 }
            ? animation.TryStart(destination, coordinated)
            : animation.TryStart(destination);
    }
    
    public static async Task<bool> TryStartFromListAsync(
        string key,
        ListViewBase list,
        object item,
        string elementName,
        ConnectedAnimationDirection direction = ConnectedAnimationDirection.Back)
    {
        if (list == null || item == null || string.IsNullOrEmpty(elementName))
            return false;

        var animation = Get(key);
        if (animation == null)
            return false;

        Configure(animation, direction);
        
        if (direction == ConnectedAnimationDirection.Back)
            list.ScrollIntoView(item, ScrollIntoViewAlignment.Default);

        return await list.TryStartConnectedAnimationAsync(animation, item, elementName);
    }
    
    public static Task<bool> TryLandBackAsync(string key, ListViewBase list, string elementName)
        => TryLandBackAsync(
            ServiceContainer.Scoped.GetService<IConnectedAnimationCoordinator>(),
            key,
            list,
            elementName);

    public static async Task<bool> TryLandBackAsync(
        IConnectedAnimationCoordinator coordinator,
        string key,
        ListViewBase list,
        string elementName)
    {
        if (!IsEnabled || coordinator == null || !coordinator.TryPeekBack(key, out var origin))
            return false;

        var item = RouteMatch.Find(list?.ItemsSource as IEnumerable ?? list?.Items, origin);
        if (item == null)
            return false;
            
        coordinator.Complete(key);

        if (await TryStartFromListAsync(key, list, item, elementName))
            return true;

        Cancel(key);
        return false;
    }
    
    private static void Configure(ConnectedAnimation animation, ConnectedAnimationDirection direction)
    {
        if (!ApiInformation.IsPropertyPresent(typeof(ConnectedAnimation).FullName, nameof(ConnectedAnimation.Configuration)))
            return;

        animation.Configuration = direction == ConnectedAnimationDirection.Back
            ? new DirectConnectedAnimationConfiguration()
            : new GravityConnectedAnimationConfiguration();
    }
}
