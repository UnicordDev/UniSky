using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniSky.Utilities;

namespace UniSky.Services;

#nullable enable

/// <summary>
/// Not all UWP devices support WebP, and cdn.bsky.app will now serve WebP by default unless
/// otherwise specified. In most cases, this is very based, it reduces bandwidth and also 
/// supports transparency, but to support older devices this service rewrites URLs where necessary
/// </summary>
internal class CdnUrlService(ITypedSettings settings) : ICdnUrlService
{
    private const string BSKY_APP_CDN_HOST = "cdn.bsky.app";

    public string? ProcessCdnUrl(string? url)
    {
        if (url == null)
            return url;

        // TODO: if hasWebp, can this always be shortcut?
        var hasWebp = settings.EnableWebP && WebPHelpers.HasWebPCodec;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        // TODO: this is bsky.app CDN specific which is Less than ideal, we shouldn't depend
        // this much on the appview
        if (uri.Host != BSKY_APP_CDN_HOST)
            return url;

        var builder = new UriBuilder(uri);
        var path = builder.Path;

        var idx = path.LastIndexOf('@');
        if (idx != -1)
            path = path[..idx];

        if (hasWebp)
        {
            path += "@webp";
        }
        else
        {
            path += "@jpeg";
        }

        builder.Path = path;

        return builder.Uri.ToString();
    }
}
