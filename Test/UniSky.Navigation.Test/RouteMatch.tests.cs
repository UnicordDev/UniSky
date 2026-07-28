using UniSky.Services.Navigation;

namespace UniSky.Navigation.Test;

/// <summary>
/// Pins down finding a destination in a list by route.
/// </summary>
/// <remarks>
/// This is the piece a previous attempt at connected animations did not have, and the reason its back
/// leg was abandoned. Landing an animation on a list item used to require the identical object that
/// left, which never survived: a post opened from a feed and the same post inside a thread are
/// unrelated instances of unrelated types, and the feed's collection was rebuilt in between. Matching
/// on the route is what makes the return leg possible at all, so it is worth holding still.
/// </remarks>
public class RouteMatchTests
{
    /// <summary>Stands in for a feed's post viewmodel.</summary>
    private sealed record FeedItem(NavigationRoute? Route) : IRoutable;

    /// <summary>
    /// Stands in for a thread's post viewmodel — deliberately a different type, because that is the
    /// case the old approach could not handle.
    /// </summary>
    private sealed record ThreadItem(NavigationRoute? Route) : IRoutable;

    private static NavigationRoute Post(string actor, string rkey)
        => NavigationRoute.Post(actor, rkey);

    [Fact]
    public void Finds_the_item_carrying_the_route()
    {
        var wanted = Post("did:plc:aaa", "3kabc");
        var items = new object[]
        {
            new FeedItem(Post("did:plc:aaa", "3kzzz")),
            new FeedItem(wanted),
            new FeedItem(Post("did:plc:bbb", "3kabc"))
        };

        var found = Assert.IsType<FeedItem>(RouteMatch.Find(items, wanted));
        Assert.Equal(wanted, found.Route);
    }

    [Fact]
    public void Matches_an_equal_route_rather_than_the_same_instance()
    {
        // The route the animation carries is never the object the list is holding — it has been
        // round-tripped through the coordinator while the list was rebuilt underneath it.
        var items = new object[] { new FeedItem(Post("did:plc:aaa", "3kabc")) };

        Assert.NotNull(RouteMatch.Find(items, Post("did:plc:aaa", "3kabc")));
    }

    [Fact]
    public void Matches_across_different_item_types()
    {
        var route = Post("did:plc:aaa", "3kabc");
        var items = new object[] { new ThreadItem(route) };

        Assert.IsType<ThreadItem>(RouteMatch.Find(items, Post("did:plc:aaa", "3kabc")));
    }

    [Fact]
    public void Returns_null_when_the_post_is_no_longer_in_the_list()
    {
        var items = new object[] { new FeedItem(Post("did:plc:aaa", "3kzzz")) };

        Assert.Null(RouteMatch.Find(items, Post("did:plc:aaa", "3kabc")));
    }

    [Fact]
    public void Ignores_items_that_are_not_routable()
    {
        var route = Post("did:plc:aaa", "3kabc");
        var items = new object[] { "a string", 42, new FeedItem(route) };

        Assert.IsType<FeedItem>(RouteMatch.Find(items, route));
    }

    [Fact]
    public void Ignores_routable_items_with_no_route()
    {
        var route = Post("did:plc:aaa", "3kabc");
        var items = new object[] { new FeedItem(null), new FeedItem(route) };

        Assert.IsType<FeedItem>(RouteMatch.Find(items, route));
    }

    [Fact]
    public void Returns_null_for_nothing_to_look_for_or_look_in()
    {
        Assert.Null(RouteMatch.Find(new object[] { new FeedItem(Post("did:plc:aaa", "3kabc")) }, null));
        Assert.Null(RouteMatch.Find(null, Post("did:plc:aaa", "3kabc")));
        Assert.Null(RouteMatch.Find(Array.Empty<object>(), Post("did:plc:aaa", "3kabc")));
    }

    [Fact]
    public void Does_not_confuse_a_post_with_its_authors_profile()
    {
        var items = new object[] { new FeedItem(NavigationRoute.Profile("did:plc:aaa")) };

        Assert.Null(RouteMatch.Find(items, Post("did:plc:aaa", "3kabc")));
    }
}
