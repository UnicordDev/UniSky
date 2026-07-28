using FishyFlip.Lexicon;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Lexicon.App.Bsky.Graph;
using FishyFlip.Models;
using UniSky.Services.Navigation;

namespace UniSky.Navigation;

/// <summary>
/// Creates routes from a lexicon objects
/// </summary>
internal static class RouteFactory
{
    public static bool TryCreateRoute(object payload, out NavigationRoute route)
    {
        route = null;

        switch (payload)
        {
            case null:
                return false;

            case NavigationRoute existing:
                route = existing;
                return true;

            case PostView { Uri: { } postUri }:
                return NavigationRoute.TryFromAtUri(postUri, out route);
            case GeneratorView { Uri: { } generatorUri }:
                return NavigationRoute.TryFromAtUri(generatorUri, out route);
            case ListView { Uri: { } listUri }:
                return NavigationRoute.TryFromAtUri(listUri, out route);
            case FeedViewPost { Post: { } feedPost }:
                return TryCreateRoute(feedPost, out route);
            case ATUri uri:
                return NavigationRoute.TryFromAtUri(uri, out route);

            case ProfileViewBasic { Did: { } basicDid }:
                route = NavigationRoute.Profile(basicDid);
                return true;
            case ProfileView { Did: { } profileDid }:
                route = NavigationRoute.Profile(profileDid);
                return true;
            case ProfileViewDetailed { Did: { } detailedDid }:
                route = NavigationRoute.Profile(detailedDid);
                return true;
            case ATIdentifier identifier:
                route = NavigationRoute.Profile(identifier);
                return true;

            default:
                return false;
        }
    }
    
    public static ATIdentifier GetActor(ATObject profile)
        => profile switch
        {
            ProfileViewBasic basic => basic.Did,
            ProfileView view => view.Did,
            ProfileViewDetailed detailed => detailed.Did,
            _ => null
        };
}
