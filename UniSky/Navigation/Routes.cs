using FishyFlip.Lexicon;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Models;
using UniSky.Services.Navigation;

namespace UniSky.Navigation;

public static class Routes
{
    public static NavigationRequest Home()
        => new(NavigationRoute.Home());

    public static NavigationRequest Notifications()
        => new(NavigationRoute.Notifications());

    public static NavigationRequest Bookmarks()
        => new(NavigationRoute.Bookmarks());

    public static NavigationRequest Search(string query)
        => new(NavigationRoute.Search(query));

    public static NavigationRequest Tag(string tag)
        => new(NavigationRoute.Tag(tag));

    public static NavigationRequest Profile(ATIdentifier actor, ATObject payload = null)
        => actor == null ? null : new NavigationRequest(NavigationRoute.Profile(actor)) { Payload = payload };

    public static NavigationRequest Profile(ATObject profile)
        => Profile(RouteFactory.GetActor(profile), profile);

    public static NavigationRequest Thread(ATUri uri, PostView payload = null)
        => NavigationRoute.TryFromAtUri(uri, out var route)
            ? new NavigationRequest(route) { Payload = payload }
            : null;

    public static NavigationRequest Thread(PostView post)
        => post?.Uri == null ? null : Thread(post.Uri, post);

    public static NavigationRequest Feed(ATUri uri, GeneratorView payload = null)
        => NavigationRoute.TryFromAtUri(uri, out var route)
            ? new NavigationRequest(route) { Payload = payload }
            : null;
    
    public static NavigationRequest For(object payload)
        => RouteFactory.TryCreateRoute(payload, out var route)
            ? new NavigationRequest(route) { Payload = payload }
            : null;
}
