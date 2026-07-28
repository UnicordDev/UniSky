using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using FishyFlip.Models;

namespace UniSky.Services.Navigation;

/// <summary>
/// A serializable identity for a destination.
/// </summary>
/// <remarks>
/// <para>
/// Mirrors bsky.app so we can do in-app links, protocol activation and toast payloads:
/// </para>
/// <code>
/// unisky:///profile/{actor}
/// unisky:///profile/{actor}/post/{rkey}
/// unisky:///profile/{actor}/feed/{rkey}
/// unisky:///profile/{actor}/lists/{rkey}
/// unisky:///tag/{tag}
/// unisky:///search?q={query}
/// unisky:///notifications
/// unisky:///bookmarks
/// </code>
/// </remarks>
public sealed class NavigationRoute : IEquatable<NavigationRoute>
{
    public const string Scheme = "unisky";

    // TODO: how many client forks? 
    private const string ExternalHost = "bsky.app";
    private const string ExternalHostWww = "www.bsky.app";

    private const string PostCollection = "app.bsky.feed.post";
    private const string FeedCollection = "app.bsky.feed.generator";
    private const string ListCollection = "app.bsky.graph.list";

    private static readonly IReadOnlyDictionary<string, string> NoQuery
        = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(0, StringComparer.Ordinal));

    private NavigationRoute(string kind, IReadOnlyList<string> segments, IReadOnlyDictionary<string, string> query)
    {
        Kind = kind;
        Segments = segments;
        Query = query;
    }

    /// <summary>
    /// One of the <see cref="RouteKinds"/>.
    /// </summary>
    public string Kind { get; }

    /// <summary>
    /// The identifying parts of the route.
    /// </summary>
    public IReadOnlyList<string> Segments { get; }

    public IReadOnlyDictionary<string, string> Query { get; }

    /// <summary>
    /// The DID or handle this route is scoped to, for profile, post, feed and list routes.
    /// </summary>
    public string? Actor
        => Kind is RouteKinds.Profile or RouteKinds.Post or RouteKinds.Feed or RouteKinds.List
            ? Segments[0]
            : null;

    /// <summary>
    /// The record key, for post, feed and list routes.
    /// </summary>
    public string? Rkey
        => Kind is RouteKinds.Post or RouteKinds.Feed or RouteKinds.List
            ? Segments[1]
            : null;

    #region Factories

    public static NavigationRoute Home()
        => new(RouteKinds.Home, [], NoQuery);

    public static NavigationRoute Notifications()
        => new(RouteKinds.Notifications, [], NoQuery);

    public static NavigationRoute Bookmarks()
        => new(RouteKinds.Bookmarks, [], NoQuery);

    public static NavigationRoute Profile(string actor)
        => new(RouteKinds.Profile, [Require(actor, nameof(actor))], NoQuery);

    public static NavigationRoute Profile(ATIdentifier actor)
        => Profile(actor?.ToString() ?? throw new ArgumentNullException(nameof(actor)));

    public static NavigationRoute Post(string actor, string rkey)
        => new(RouteKinds.Post, [Require(actor, nameof(actor)), Require(rkey, nameof(rkey))], NoQuery);

    public static NavigationRoute Feed(string actor, string rkey)
        => new(RouteKinds.Feed, [Require(actor, nameof(actor)), Require(rkey, nameof(rkey))], NoQuery);

    public static NavigationRoute List(string actor, string rkey)
        => new(RouteKinds.List, [Require(actor, nameof(actor)), Require(rkey, nameof(rkey))], NoQuery);

    public static NavigationRoute Tag(string tag)
        => new(RouteKinds.Tag, [Require(tag, nameof(tag))], NoQuery);

    public static NavigationRoute Search(string query)
        => new(RouteKinds.Search, [], Single("q", query ?? string.Empty));

    /// <summary>
    /// Maps an <c>at://</c> record URI onto the matching route, using its collection NSID.
    /// </summary>
    public static bool TryFromAtUri(ATUri? uri, [NotNullWhen(true)] out NavigationRoute? route)
    {
        route = null;
        if (uri?.Identity is not string identity || identity.Length == 0)
            return false;

        // pathname is empty for a bare at://did, which identifies the repo itself.
        var parts = uri.Pathname.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            route = Profile(identity);
            return true;
        }

        // exactly a collection and an rkey, same reasoning as the path parser above.
        if (parts.Length != 2)
            return false;

        route = parts[0] switch
        {
            PostCollection => Post(identity, parts[1]),
            FeedCollection => Feed(identity, parts[1]),
            ListCollection => List(identity, parts[1]),
            _ => null
        };

        return route != null;
    }

    private static string Require(string value, string name)
        => string.IsNullOrEmpty(value) ? throw new ArgumentException("Route segment may not be empty.", name) : value;

    private static IReadOnlyDictionary<string, string> Single(string key, string value)
        => new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(1, StringComparer.Ordinal) { [key] = value });

    #endregion

    #region Formatting

    /// <summary>
    /// The in-app form, e.g. <c>unisky:///profile/did:plc:xyz/post/3kabc</c>.
    /// </summary>
    public Uri ToUri()
        => new($"{Scheme}://{BuildPathAndQuery()}");

    /// <summary>
    /// The shareable web form, e.g. <c>https://bsky.app/profile/did:plc:xyz/post/3kabc</c>.
    /// </summary>
    public Uri ToExternalUri()
        => new($"https://{ExternalHost}{BuildPathAndQuery()}");

    /// <summary>
    /// The <c>at://</c> record URI, for the routes that identify a repo record.
    /// </summary>
    public bool TryToAtUri([NotNullWhen(true)] out ATUri? uri)
    {
        var collection = Kind switch
        {
            RouteKinds.Post => PostCollection,
            RouteKinds.Feed => FeedCollection,
            RouteKinds.List => ListCollection,
            _ => null
        };

        if (collection == null)
        {
            // A profile route still names a repo, even though it names no record within it.
            if (Kind == RouteKinds.Profile)
                return ATUri.TryCreate($"at://{Actor}", out uri);

            uri = null;
            return false;
        }

        return ATUri.TryCreate($"at://{Actor}/{collection}/{Rkey}", out uri);
    }

    public string ToCanonicalString()
        => ToUri().ToString();

    public override string ToString()
        => ToCanonicalString();

    private string BuildPathAndQuery()
    {
        var builder = new StringBuilder();

        switch (Kind)
        {
            case RouteKinds.Home:
                builder.Append('/');
                break;
            case RouteKinds.Notifications:
            case RouteKinds.Bookmarks:
            case RouteKinds.Search:
                builder.Append('/').Append(Kind);
                break;
            case RouteKinds.Tag:
                builder.Append("/tag/").Append(Escape(Segments[0]));
                break;
            case RouteKinds.Profile:
                builder.Append("/profile/").Append(Escape(Segments[0]));
                break;
            case RouteKinds.Post:
                builder.Append("/profile/").Append(Escape(Segments[0])).Append("/post/").Append(Escape(Segments[1]));
                break;
            case RouteKinds.Feed:
                builder.Append("/profile/").Append(Escape(Segments[0])).Append("/feed/").Append(Escape(Segments[1]));
                break;
            case RouteKinds.List:
                builder.Append("/profile/").Append(Escape(Segments[0])).Append("/lists/").Append(Escape(Segments[1]));
                break;
            default:
                throw new InvalidOperationException($"Unknown route kind '{Kind}'.");
        }

        if (Query.Count > 0)
        {
            builder.Append('?');
            var first = true;
            foreach (var pair in Query.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                if (!first)
                    builder.Append('&');
                first = false;

                builder.Append(Uri.EscapeDataString(pair.Key))
                       .Append('=')
                       .Append(Uri.EscapeDataString(pair.Value));
            }
        }

        return builder.ToString();
    }


    private static string Escape(string segment)
    {
        var builder = new StringBuilder(segment.Length);
        foreach (var c in segment)
        {
            if (c is '%' or '/' or '?' or '#' or '&' || char.IsWhiteSpace(c))
                builder.Append('%').Append(((int)c).ToString("X2"));
            else
                builder.Append(c);
        }

        return builder.ToString();
    }

    #endregion

    #region Parsing

    /// <summary>
    /// Parses an in-app <c>unisky:</c> URI, a bsky.app web link, or an <c>at://</c> record URI.
    /// </summary>
    public static bool TryParse(string? value, [NotNullWhen(true)] out NavigationRoute? route)
    {
        route = null;

        // IsNullOrWhiteSpace doesn't tell the nullability analyzer jack shit on .NET Standard 2.0
        if (value is null || value.Trim().Length == 0)
            return false;

        if (value.StartsWith("at://", StringComparison.OrdinalIgnoreCase))
            return ATUri.TryCreate(value, out var atUri) && TryFromAtUri(atUri, out route);

        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && TryParse(uri, out route);
    }

    public static bool TryParse(Uri? uri, [NotNullWhen(true)] out NavigationRoute? route)
    {
        route = null;
        if (uri == null || !uri.IsAbsoluteUri)
            return false;

        if (uri.Scheme.Equals("at", StringComparison.OrdinalIgnoreCase))
            return TryParse(uri.OriginalString, out route);

        var recognised = uri.Scheme.Equals(Scheme, StringComparison.OrdinalIgnoreCase)
            || ((uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                 || uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
                && (uri.Host.Equals(ExternalHost, StringComparison.OrdinalIgnoreCase)
                    || uri.Host.Equals(ExternalHostWww, StringComparison.OrdinalIgnoreCase)));

        if (!recognised)
            return false;

        var segments = uri.AbsolutePath
            .Split(['/'], StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();

        var query = ParseQuery(uri.Query);

        if (segments.Length == 0)
        {
            route = Home();
            return true;
        }
        
        switch (segments[0].ToLowerInvariant())
        {
            case RouteKinds.Notifications when segments.Length == 1:
                route = Notifications();
                return true;

            case RouteKinds.Bookmarks when segments.Length == 1:
                route = Bookmarks();
                return true;

            case RouteKinds.Search when segments.Length == 1:
                route = Search(query.TryGetValue("q", out var q) ? q : string.Empty);
                return true;

            case RouteKinds.Tag when segments.Length == 2:
                route = Tag(segments[1]);
                return true;

            case RouteKinds.Profile when segments.Length == 2:
                route = Profile(segments[1]);
                return true;

            case RouteKinds.Profile when segments.Length == 4:
                route = segments[2].ToLowerInvariant() switch
                {
                    "post" => Post(segments[1], segments[3]),
                    "feed" => Feed(segments[1], segments[3]),
                    "lists" or "list" => List(segments[1], segments[3]),
                    _ => null
                };

                return route != null;

            default:
                return false;
        }
    }

    private static IReadOnlyDictionary<string, string> ParseQuery(string query)
    {
        if (string.IsNullOrEmpty(query) || query == "?")
            return NoQuery;

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in query.TrimStart('?').Split(['&'], StringSplitOptions.RemoveEmptyEntries))
        {
            var split = pair.IndexOf('=');
            if (split < 0)
                result[Uri.UnescapeDataString(pair)] = string.Empty;
            else
                result[Uri.UnescapeDataString(pair.Substring(0, split))] = Uri.UnescapeDataString(pair.Substring(split + 1));
        }

        return new ReadOnlyDictionary<string, string>(result);
    }

    #endregion

    #region Equality

    public bool Equals(NavigationRoute? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        if (!string.Equals(Kind, other.Kind, StringComparison.Ordinal))
            return false;

        if (Segments.Count != other.Segments.Count || Query.Count != other.Query.Count)
            return false;

        for (var i = 0; i < Segments.Count; i++)
        {
            if (!string.Equals(Segments[i], other.Segments[i], StringComparison.Ordinal))
                return false;
        }

        foreach (var pair in Query)
        {
            if (!other.Query.TryGetValue(pair.Key, out var value) || !string.Equals(pair.Value, value, StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
        => Equals(obj as NavigationRoute);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = Kind.GetHashCode();
            foreach (var segment in Segments)
                hash = (hash * 397) ^ segment.GetHashCode();
            foreach (var pair in Query.OrderBy(p => p.Key, StringComparer.Ordinal))
                hash = (hash * 397) ^ pair.Key.GetHashCode() ^ pair.Value.GetHashCode();

            return hash;
        }
    }

    public static bool operator ==(NavigationRoute? left, NavigationRoute? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(NavigationRoute? left, NavigationRoute? right)
        => !(left == right);

    #endregion
}
