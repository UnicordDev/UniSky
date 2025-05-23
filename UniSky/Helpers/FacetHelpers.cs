﻿// <copyright file="Facet.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using FishyFlip.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System;
using FishyFlip.Lexicon.App.Bsky.Richtext;
using FishyFlip.Lexicon;
using FishyFlip.Lexicon.App.Bsky.Actor;

namespace UniSky.Helpers;

/// <summary>
/// Facets.
/// </summary>
public class FacetHelpers
{
    /// <summary>
    /// Creates a facet with a link feature.
    /// </summary>
    /// <param name="start">The start index of the link.</param>
    /// <param name="end">The end index of the link.</param>
    /// <param name="uri">The URI of the link.</param>
    /// <returns>The created facet.</returns>
    public static Facet CreateFacetLink(int start, int end, string uri)
    {
        var link = new Link();
        link.Uri = uri;
        var facet = new Facet();
        facet.Features = new List<ATObject> { link };
        facet.Index = new ByteSlice();
        facet.Index.ByteStart = start;
        facet.Index.ByteEnd = end;
        return facet;
    }

    /// <summary>
    /// Creates a facet with a hashtag feature.
    /// </summary>
    /// <param name="start">The start index of the hashtag.</param>
    /// <param name="end">The end index of the hashtag.</param>
    /// <param name="hashtag">The hashtag value.</param>
    /// <returns>The created facet.</returns>
    public static Facet CreateFacetHashtag(int start, int end, string hashtag)
    {
        var facet = new Facet();
        var hashtagFeature = new Tag();
        hashtagFeature.TagValue = hashtag;
        facet.Features = new List<ATObject> { hashtagFeature };
        facet.Index = new ByteSlice();
        facet.Index.ByteStart = start;
        facet.Index.ByteEnd = end;
        return facet;
    }

    /// <summary>
    /// Creates a facet with a mention feature.
    /// </summary>
    /// <param name="start">The start index of the mention.</param>
    /// <param name="end">The end index of the mention.</param>
    /// <param name="mention">The mention value.</param>
    /// <returns>The created facet.</returns>
    public static Facet CreateFacetMention(int start, int end, ATDid mention)
    {
        var facet = new Facet();
        var mentionFeature = new Mention();
        mentionFeature.Did = mention;
        facet.Features = new List<ATObject> { mentionFeature };
        facet.Index = new ByteSlice();
        facet.Index.ByteStart = start;
        facet.Index.ByteEnd = end;
        return facet;
    }

    /// <summary>
    /// Creates an array of facets with link features for the URIs in the specified post.
    /// </summary>
    /// <param name="post">Post text.</param>
    /// <returns>Array of Facets.</returns>
    public static Facet[] ForUris(string post)
    {
        var facets = new List<Facet>();
        var matches = Regex.Matches(post, @"(https?://[^\s]+)");
        var postBytes = Encoding.UTF8.GetBytes(post);
        var startIndex = 0;
        foreach (Match match in matches)
        {
            var matchBytes = Encoding.UTF8.GetBytes(match.Value);
            var position = FindPattern(postBytes, matchBytes, startIndex);
            startIndex = position.End;
            facets.Add(CreateFacetLink(position.Start, position.End, match.Value));
        }

        return [.. facets];
    }

    /// <summary>
    /// Creates an array of facets with link features for the URIs in the specified post.
    /// </summary>
    /// <param name="post">Post text.</param>
    /// <param name="baseText">Text to embed with link.</param>
    /// <param name="uri">Link Uri.</param>
    /// <returns>Array of Facets.</returns>
    public static Facet[] ForUris(string post, string baseText, string uri)
    {
        var facets = new List<Facet>();
        var matches = Regex.Matches(post, baseText);
        var postBytes = Encoding.UTF8.GetBytes(post);
        var startIndex = 0;
        foreach (Match match in matches)
        {
            var matchBytes = Encoding.UTF8.GetBytes(match.Value);
            var position = FindPattern(postBytes, matchBytes, startIndex);
            startIndex = position.End;
            facets.Add(CreateFacetLink(position.Start, position.End, uri));
        }

        return [.. facets];
    }

    /// <summary>
    /// Creates an array of facets with hashtag features for the hashtags in the specified post.
    /// </summary>
    /// <param name="post">Post text.</param>
    /// <returns>Array of Facets.</returns>
    public static Facet[] ForHashtags(string post)
    {
        var facets = new List<Facet>();

        // Match all hashtags in the post that are not part of a URL.
        var matches = Regex.Matches(post, @"(?<![@\w/])#(?!\s)[\w\u0080-\uFFFF]+");
        var postBytes = Encoding.UTF8.GetBytes(post);
        var startIndex = 0;
        foreach (Match match in matches)
        {
            var matchBytes = Encoding.UTF8.GetBytes(match.Value);
            var position = FindPattern(postBytes, matchBytes, startIndex);

            var hashtag = match.Value;

            if (hashtag.StartsWith("#"))
            {
                hashtag = hashtag.Substring(1);
            }

            facets.Add(CreateFacetHashtag(position.Start, position.End, hashtag));
            startIndex = position.End;
        }

        return [.. facets];
    }

    /// <summary>
    /// Creates an array of facets with mention features for the mentions in the specified post.
    /// </summary>
    /// <param name="post">Post text.</param>
    /// <param name="actors">Array of actors profiles.</param>
    /// <returns>Array of Facets.</returns>
    public static Facet[] ForMentions(string post, FacetActorIdentifier[] actors)
    {
        var facets = new List<Facet>();

        // Match all mentions in the post that are not part of a URL.
        var matches = Regex.Matches(post, @"@(?!http)[a-zA-Z0-9][-a-zA-Z0-9_.]{1,}");
        var postBytes = Encoding.UTF8.GetBytes(post);
        var startIndex = 0;
        foreach (Match match in matches)
        {
            var matchBytes = Encoding.UTF8.GetBytes(match.Value);
            var position = FindPattern(postBytes, matchBytes, startIndex);

            var mention = match.Value;
            if (mention.StartsWith("@"))
            {
                mention = mention.Substring(1);
            }

            if (string.IsNullOrEmpty(mention))
            {
                continue;
            }

            var actor = actors.FirstOrDefault(n => n.Handle.ToString() == mention);
            if (actor?.Did is not null)
            {
                facets.Add(CreateFacetMention(position.Start, position.End, actor.Did));
            }

            startIndex = position.End;
        }

        return [.. facets];
    }

    /// <summary>
    /// Creates an array of facets with mention features for the mentions in the specified post.
    /// </summary>
    /// <param name="post">Post text.</param>
    /// <param name="actors">Actor profiles.</param>
    /// <returns>Array of Facets.</returns>
    public static Facet[] ForMentions(string post, ProfileViewDetailed[] actors)
    {
        var actorList = new List<FacetActorIdentifier>();
        foreach (var actor in actors)
        {
            if (actor.Handle is null)
            {
                continue;
            }

            if (actor.Handle is null || actor.Did is null)
            {
                continue;
            }

            actorList.Add(new FacetActorIdentifier(actor.Handle, actor.Did));
        }

        return ForMentions(post, actorList.ToArray());
    }

    /// <summary>
    /// Creates an array of facets with mention features for the mentions in the specified post.
    /// </summary>
    /// <param name="post">Post text.</param>
    /// <param name="actors">Actor profiles.</param>
    /// <returns>Array of Facets.</returns>
    public static Facet[] ForMentions(string post, ProfileViewBasic[] actors)
    {
        var actorList = new List<FacetActorIdentifier>();
        foreach (var actor in actors)
        {
            if (actor.Handle is null)
            {
                continue;
            }

            if (actor.Handle is null || actor.Did is null)
            {
                continue;
            }

            actorList.Add(new FacetActorIdentifier(actor.Handle, actor.Did));
        }

        return ForMentions(post, actorList.ToArray());
    }

    /// <summary>
    /// Creates an array of facets with mention features for the mentions in the specified post.
    /// </summary>
    /// <param name="post">Post text.</param>
    /// <param name="actor">Actor profiles.</param>
    /// <returns>Array of Facets.</returns>
    public static Facet[] ForMentions(string post, ProfileViewBasic actor)
        => ForMentions(post, [actor]);

    /// <summary>
    /// Parses a post and returns an array of facets.
    /// </summary>
    /// <param name="post">The post text.</param>
    /// <param name="actors">Optional list of Actor DID values, used for creating Mention Facets.</param>
    /// <returns>Array of Facets.</returns>
    public static Facet[] Parse(string post, ProfileViewBasic[]? actors = null)
    {
        var uriFacets = ForUris(post);
        var hashtagFacets = ForHashtags(post);
        var mentionFacets = ForMentions(post, actors ?? []);
        return [.. uriFacets, .. hashtagFacets, .. mentionFacets];
    }

    /// <summary>
    /// Parses a post and returns an array of facets.
    /// </summary>
    /// <param name="post">The post text.</param>
    /// <param name="actors">Optional list of Actor DID values, used for creating Mention Facets.</param>
    /// <returns>Array of Facets.</returns>
    public static Facet[] Parse(string post, ProfileViewDetailed[]? actors = null)
    {
        var uriFacets = ForUris(post);
        var hashtagFacets = ForHashtags(post);
        var mentionFacets = ForMentions(post, actors ?? []);
        return [.. uriFacets, .. hashtagFacets, .. mentionFacets];
    }


    /// <summary>
    /// Gets the handles from a post text.
    /// </summary>
    /// <param name="post">Text of the post.</param>
    /// <returns>Array of ATHandle.</returns>
    public static ATHandle[] HandlesForMentions(string post)
        => ATHandle.FromPostText(post);

    private static (int Start, int End) FindPattern(byte[] source, byte[] pattern, int startIndex = 0)
    {
        return FindPattern(source.AsSpan(), pattern.AsSpan(), startIndex);
    }

    private static (int Start, int End) FindPattern(ReadOnlySpan<byte> source, ReadOnlySpan<byte> pattern, int startIndex = 0)
    {
        if (pattern.IsEmpty || pattern.Length > source.Length)
        {
            return (0, 0);
        }

        for (int i = startIndex; i <= source.Length - pattern.Length; i++)
        {
            if (source.Slice(i, pattern.Length).SequenceEqual(pattern))
            {
                return (i, i + pattern.Length);
            }
        }

        return (0, 0);
    }
}

