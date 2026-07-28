using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using FishyFlip.Lexicon.App.Bsky.Embed;
using FishyFlip.Models;

namespace UniSky.Helpers;

/// <summary>
/// Adapts <c>app.bsky.embed.gallery</c> embeds, which FishyFlip has no lexicon types for, into the
/// <see cref="ViewImages"/> shape the rest of the app already understands.
/// </summary>
/// <remarks>
/// Unrecognised embeds arrive as an <see cref="UnknownATObject"/> with the original payload intact in
/// <see cref="UnknownATObject.Json"/>, so we can parse it ourselves and hand back FishyFlip's own
/// types. Everything downstream (the carousel, the fullscreen gallery, the connected animation) then
/// works without knowing gallery exists.
///
/// Note the field names differ from <c>app.bsky.embed.images</c>: the array is <c>items</c> rather
/// than <c>images</c>, and the thumbnail is <c>thumbnail</c> rather than <c>thumb</c>.
/// </remarks>
public static class GalleryEmbed
{
    public const string RecordType = "app.bsky.embed.gallery";
    public const string ViewType = "app.bsky.embed.gallery#view";
    public const string ImageType = "app.bsky.embed.gallery#image";
    public const string ViewImageType = "app.bsky.embed.gallery#viewImage";

    /// <summary>
    /// Converts a hydrated <c>app.bsky.embed.gallery#view</c>, as returned by feeds and threads, where
    /// the CDN URLs are already absolute.
    /// </summary>
    public static bool TryCreateViewImages(UnknownATObject unknown, out ViewImages images)
        => TryCreateViewImages(unknown, ViewImageType, null, out images);

    /// <summary>
    /// Converts the record form of <c>app.bsky.embed.gallery</c>, as found on notification subjects,
    /// building CDN URLs from each blob reference and the post author.
    /// </summary>
    public static bool TryCreateViewImages(UnknownATObject unknown, ATIdentifier id, out ViewImages images)
        => TryCreateViewImages(unknown, ImageType, id, out images);

    private static bool TryCreateViewImages(UnknownATObject unknown, string itemType, ATIdentifier id, out ViewImages images)
    {
        images = null;

        if (string.IsNullOrEmpty(unknown?.Json))
            return false;

        try
        {
            using var document = JsonDocument.Parse(unknown.Json);
            if (!document.RootElement.TryGetProperty("items", out var items) ||
                items.ValueKind != JsonValueKind.Array)
                return false;

            var parsed = new List<ViewImage>();
            foreach (var item in items.EnumerateArray())
            {
                // the items array is a union, and the lexicon explicitly reserves the right to add
                // other media types to it. render the images we understand rather than nothing.
                if (item.ValueKind != JsonValueKind.Object || GetString(item, "$type") != itemType)
                    continue;

                if (TryCreateViewImage(item, id, out var image))
                    parsed.Add(image);
            }

            if (parsed.Count == 0)
                return false;

            images = new ViewImages(parsed);
            return true;
        }
        catch (Exception ex)
        {
            // a malformed embed shouldn't take the whole feed item with it
            Debug.WriteLine($"Failed to parse gallery embed: {ex}");
            return false;
        }
    }

    private static bool TryCreateViewImage(JsonElement item, ATIdentifier id, out ViewImage image)
    {
        image = null;

        string thumbnail, fullsize;
        if (id is null)
        {
            thumbnail = GetString(item, "thumbnail");
            fullsize = GetString(item, "fullsize");
        }
        else
        {
            if (!item.TryGetProperty("image", out var blob) ||
                !blob.TryGetProperty("ref", out var reference) ||
                GetString(reference, "$link") is not { Length: > 0 } link)
                return false;

            thumbnail = $"https://cdn.bsky.app/img/feed_thumbnail/plain/{id}/{link}";
            fullsize = $"https://cdn.bsky.app/img/feed_fullsize/plain/{id}/{link}";
        }

        if (string.IsNullOrEmpty(thumbnail) || string.IsNullOrEmpty(fullsize))
            return false;

        AspectRatio aspectRatio = null;
        if (item.TryGetProperty("aspectRatio", out var ratio) &&
            ratio.ValueKind == JsonValueKind.Object &&
            ratio.TryGetProperty("width", out var width) &&
            ratio.TryGetProperty("height", out var height) &&
            width.TryGetInt64(out var w) && height.TryGetInt64(out var h))
        {
            aspectRatio = new AspectRatio(w, h);
        }

        image = new ViewImage(thumbnail, fullsize, GetString(item, "alt") ?? string.Empty, aspectRatio);
        return true;
    }

    private static string GetString(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
