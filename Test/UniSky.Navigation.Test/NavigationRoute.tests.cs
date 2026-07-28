using FishyFlip.Models;
using UniSky.Services.Navigation;

namespace UniSky.Navigation.Test;

public class NavigationRouteTests
{
    private const string Did = "did:plc:vwzwgnygau7ed7b7wt5ux7y2";
    private const string Handle = "wamwoowam.co.uk";
    private const string Rkey = "3kabcxyz1a22b";

    #region Parsing

    [Theory]
    [InlineData("unisky:///profile/did:plc:vwzwgnygau7ed7b7wt5ux7y2")]
    [InlineData("https://bsky.app/profile/did:plc:vwzwgnygau7ed7b7wt5ux7y2")]
    [InlineData("https://www.bsky.app/profile/did:plc:vwzwgnygau7ed7b7wt5ux7y2")]
    [InlineData("http://bsky.app/profile/did:plc:vwzwgnygau7ed7b7wt5ux7y2")]
    [InlineData("at://did:plc:vwzwgnygau7ed7b7wt5ux7y2")]
    public void EveryFormOfAProfileLinkParsesToTheSameRoute(string input)
    {
        Assert.True(NavigationRoute.TryParse(input, out var route));
        Assert.Equal(NavigationRoute.Profile(Did), route);
        Assert.Equal(RouteKinds.Profile, route.Kind);
        Assert.Equal(Did, route.Actor);
    }

    [Theory]
    [InlineData("unisky:///profile/did:plc:vwzwgnygau7ed7b7wt5ux7y2/post/3kabcxyz1a22b")]
    [InlineData("https://bsky.app/profile/did:plc:vwzwgnygau7ed7b7wt5ux7y2/post/3kabcxyz1a22b")]
    [InlineData("at://did:plc:vwzwgnygau7ed7b7wt5ux7y2/app.bsky.feed.post/3kabcxyz1a22b")]
    public void EveryFormOfAPostLinkParsesToTheSameRoute(string input)
    {
        Assert.True(NavigationRoute.TryParse(input, out var route));
        Assert.Equal(NavigationRoute.Post(Did, Rkey), route);
        Assert.Equal(Did, route.Actor);
        Assert.Equal(Rkey, route.Rkey);
    }

    [Fact]
    public void HandlesAreAcceptedWhereADidWouldBe()
    {
        Assert.True(NavigationRoute.TryParse($"https://bsky.app/profile/{Handle}/post/{Rkey}", out var route));
        Assert.Equal(NavigationRoute.Post(Handle, Rkey), route);
    }

    [Theory]
    [InlineData("at://did:plc:vwzwgnygau7ed7b7wt5ux7y2/app.bsky.feed.generator/3kabcxyz1a22b", RouteKinds.Feed)]
    [InlineData("at://did:plc:vwzwgnygau7ed7b7wt5ux7y2/app.bsky.graph.list/3kabcxyz1a22b", RouteKinds.List)]
    public void CollectionNsidSelectsTheRouteKind(string input, string expectedKind)
    {
        Assert.True(NavigationRoute.TryParse(input, out var route));
        Assert.Equal(expectedKind, route.Kind);
        Assert.Equal(Rkey, route.Rkey);
    }

    [Theory]
    [InlineData("unisky:///notifications", RouteKinds.Notifications)]
    [InlineData("unisky:///bookmarks", RouteKinds.Bookmarks)]
    [InlineData("unisky:///", RouteKinds.Home)]
    [InlineData("https://bsky.app/", RouteKinds.Home)]
    public void StandaloneDestinationsParse(string input, string expectedKind)
    {
        Assert.True(NavigationRoute.TryParse(input, out var route));
        Assert.Equal(expectedKind, route.Kind);
    }

    [Fact]
    public void SearchCarriesItsQuery()
    {
        Assert.True(NavigationRoute.TryParse("unisky:///search?q=hello%20world", out var route));
        Assert.Equal(RouteKinds.Search, route.Kind);
        Assert.Equal("hello world", route.Query["q"]);
    }

    [Fact]
    public void ListsAcceptBothSpellings()
    {
        Assert.True(NavigationRoute.TryParse($"https://bsky.app/profile/{Did}/lists/{Rkey}", out var plural));
        Assert.True(NavigationRoute.TryParse($"https://bsky.app/profile/{Did}/list/{Rkey}", out var singular));
        Assert.Equal(plural, singular);
    }

    #endregion

    #region Malformed input

    [Theory]
    // The case that currently throws IndexOutOfRangeException in ProfilePage.HandleUniskyProtocol.
    [InlineData("unisky:///profile")]
    [InlineData("unisky:///profile/")]
    [InlineData("unisky:///tag")]
    [InlineData("unisky:///profile/did:plc:abc/post")]
    [InlineData("unisky:///profile/did:plc:abc/nonsense/xyz")]
    [InlineData("unisky:///nonsense")]
    [InlineData("https://example.com/profile/did:plc:abc")]
    [InlineData("at://")]
    [InlineData("not a uri at all")]
    [InlineData("")]
    [InlineData(null)]
    public void MalformedInputIsRejectedWithoutThrowing(string? input)
    {
        Assert.False(NavigationRoute.TryParse(input!, out var route));
        Assert.Null(route);
    }

    [Theory]
    [InlineData("unisky:///profile/did:plc:abc/post/3kabc/extra")]
    [InlineData("unisky:///tag/cats/extra")]
    [InlineData("unisky:///notifications/extra")]
    [InlineData("https://bsky.app/profile/did:plc:abc/post/3kabc/nonsense")]
    public void TrailingJunkIsRejectedRatherThanIgnored(string input)
    {
        // Silently discarding path segments would let two different links resolve to the same
        // destination, which matters when the link came from outside the app.
        Assert.False(NavigationRoute.TryParse(input, out var route));
        Assert.Null(route);
    }

    [Theory]
    [InlineData("at://did:plc:abc/app.bsky.feed.post")]
    [InlineData("at://did:plc:abc/unknown.collection/3kabc")]
    [InlineData("at://not a did/app.bsky.feed.post/3kabc")]
    public void UnusableRecordUrisAreRejected(string input)
    {
        Assert.False(NavigationRoute.TryParse(input, out var route));
        Assert.Null(route);
    }

    #endregion

    #region Normalisation

    [Theory]
    [InlineData("UNISKY:///profile/did:plc:vwzwgnygau7ed7b7wt5ux7y2")]
    [InlineData("HTTPS://BSKY.APP/profile/did:plc:vwzwgnygau7ed7b7wt5ux7y2")]
    [InlineData("unisky:///PROFILE/did:plc:vwzwgnygau7ed7b7wt5ux7y2")]
    [InlineData("unisky:///profile/did:plc:vwzwgnygau7ed7b7wt5ux7y2/")]
    public void SchemeHostAndKeywordsAreCaseAndSlashInsensitive(string input)
    {
        Assert.True(NavigationRoute.TryParse(input, out var route));
        Assert.Equal(NavigationRoute.Profile(Did), route);
    }

    [Fact]
    public void ActorCaseIsPreserved()
    {
        // Handles are compared case-insensitively upstream, but we must not silently rewrite what
        // the user linked to.
        Assert.True(NavigationRoute.TryParse("https://bsky.app/profile/WamWooWam.co.uk", out var route));
        Assert.Equal("WamWooWam.co.uk", route.Actor);
    }

    [Fact]
    public void PercentEncodedSegmentsAreDecoded()
    {
        Assert.True(NavigationRoute.TryParse("unisky:///profile/did%3Aplc%3Avwzwgnygau7ed7b7wt5ux7y2", out var route));
        Assert.Equal(NavigationRoute.Profile(Did), route);
    }

    [Fact]
    public void QueryStringsOnNonSearchRoutesAreDropped()
    {
        // A tracking parameter on a shared link must not make it a different destination.
        Assert.True(NavigationRoute.TryParse($"https://bsky.app/profile/{Did}/post/{Rkey}?ref=twitter", out var route));
        Assert.Equal(NavigationRoute.Post(Did, Rkey), route);
        Assert.Empty(route.Query);
    }

    [Fact]
    public void FragmentsAreIgnored()
    {
        Assert.True(NavigationRoute.TryParse($"unisky:///profile/{Did}#bio", out var route));
        Assert.Equal(NavigationRoute.Profile(Did), route);
    }

    [Fact]
    public void SearchWithNoQueryIsStillASearch()
    {
        Assert.True(NavigationRoute.TryParse("unisky:///search", out var route));
        Assert.Equal(RouteKinds.Search, route.Kind);
        Assert.Equal(string.Empty, route.Query["q"]);
        Assert.Equal(route, NavigationRoute.Search(string.Empty));
    }

    [Fact]
    public void RecordUrisNameTheirRepoByHandleToo()
    {
        Assert.True(NavigationRoute.TryParse($"at://{Handle}/app.bsky.feed.post/{Rkey}", out var route));
        Assert.Equal(NavigationRoute.Post(Handle, Rkey), route);
    }

    #endregion

    #region Guards

    [Fact]
    public void EmptySegmentsAreRejectedAtConstruction()
    {
        Assert.Throws<ArgumentException>(() => NavigationRoute.Profile(string.Empty));
        Assert.Throws<ArgumentException>(() => NavigationRoute.Post(Did, string.Empty));
        Assert.Throws<ArgumentException>(() => NavigationRoute.Tag(string.Empty));
        Assert.Throws<ArgumentNullException>(() => NavigationRoute.Profile((ATIdentifier)null!));
    }

    [Fact]
    public void SegmentsAreNotMutableByCallers()
    {
        var route = NavigationRoute.Post(Did, Rkey);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)route.Segments).Add("oops"));
    }

    #endregion

    #region Formatting

    [Theory]
    [InlineData("unisky:///profile/did:plc:vwzwgnygau7ed7b7wt5ux7y2")]
    [InlineData("unisky:///profile/did:plc:vwzwgnygau7ed7b7wt5ux7y2/post/3kabcxyz1a22b")]
    [InlineData("unisky:///notifications")]
    [InlineData("unisky:///bookmarks")]
    [InlineData("unisky:///tag/catstodon")]
    public void CanonicalStringsRoundTrip(string canonical)
    {
        Assert.True(NavigationRoute.TryParse(canonical, out var route));
        Assert.Equal(canonical, route.ToCanonicalString());

        Assert.True(NavigationRoute.TryParse(route.ToCanonicalString(), out var again));
        Assert.Equal(route, again);
    }

    [Fact]
    public void SearchRoundTripsThroughItsQuery()
    {
        var route = NavigationRoute.Search("hello world");

        Assert.True(NavigationRoute.TryParse(route.ToCanonicalString(), out var parsed));
        Assert.Equal(route, parsed);
        Assert.Equal("hello world", parsed.Query["q"]);
    }

    [Fact]
    public void TagsWithAwkwardCharactersSurviveTheRoundTrip()
    {
        var route = NavigationRoute.Tag("a/b?c#d e");

        Assert.True(NavigationRoute.TryParse(route.ToCanonicalString(), out var parsed));
        Assert.Equal(route, parsed);
        Assert.Equal("a/b?c#d e", parsed.Segments[0]);
    }

    [Fact]
    public void ExternalUriIsTheShareableWebForm()
    {
        var route = NavigationRoute.Post(Handle, Rkey);
        Assert.Equal($"https://bsky.app/profile/{Handle}/post/{Rkey}", route.ToExternalUri().ToString());
    }

    [Fact]
    public void ExternalUriParsesBackToTheSameRoute()
    {
        var route = NavigationRoute.Post(Did, Rkey);

        Assert.True(NavigationRoute.TryParse(route.ToExternalUri(), out var parsed));
        Assert.Equal(route, parsed);
    }

    #endregion

    #region at:// conversion

    [Fact]
    public void PostRoutesConvertBackToARecordUri()
    {
        var route = NavigationRoute.Post(Did, Rkey);

        Assert.True(route.TryToAtUri(out var uri));
        Assert.Equal($"at://{Did}/app.bsky.feed.post/{Rkey}", uri.ToString());
    }

    [Fact]
    public void ProfileRoutesConvertToTheRepoUri()
    {
        Assert.True(NavigationRoute.Profile(Did).TryToAtUri(out var uri));
        Assert.Equal($"at://{Did}/", uri.ToString());
    }

    [Fact]
    public void RoutesWithNoRecordHaveNoRecordUri()
    {
        Assert.False(NavigationRoute.Notifications().TryToAtUri(out _));
        Assert.False(NavigationRoute.Tag("cats").TryToAtUri(out _));
    }

    [Fact]
    public void AtUriRoundTripsThroughTheRoute()
    {
        var original = ATUri.Create($"at://{Did}/app.bsky.feed.post/{Rkey}");

        Assert.True(NavigationRoute.TryFromAtUri(original, out var route));
        Assert.True(route.TryToAtUri(out var roundTripped));
        Assert.Equal(original.ToString(), roundTripped.ToString());
    }

    #endregion

    #region Equality

    [Fact]
    public void EqualityIsByValue()
    {
        Assert.Equal(NavigationRoute.Post(Did, Rkey), NavigationRoute.Post(Did, Rkey));
        Assert.Equal(NavigationRoute.Post(Did, Rkey).GetHashCode(), NavigationRoute.Post(Did, Rkey).GetHashCode());
        Assert.True(NavigationRoute.Post(Did, Rkey) == NavigationRoute.Post(Did, Rkey));
    }

    [Fact]
    public void DifferentDestinationsAreNotEqual()
    {
        Assert.NotEqual(NavigationRoute.Post(Did, Rkey), NavigationRoute.Post(Did, "3kdifferent"));
        Assert.NotEqual(NavigationRoute.Profile(Did), NavigationRoute.Profile(Handle));
        Assert.NotEqual(NavigationRoute.Search("cats"), NavigationRoute.Search("dogs"));

        // Same segments, different kind.
        Assert.NotEqual(NavigationRoute.Feed(Did, Rkey), NavigationRoute.List(Did, Rkey));
    }

    [Fact]
    public void NullComparesSafely()
    {
        NavigationRoute? route = null;

        Assert.True(route == null);
        Assert.False(NavigationRoute.Home() == null);
        Assert.False(NavigationRoute.Home()!.Equals(null));
    }

    #endregion
}
