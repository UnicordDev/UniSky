using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FishyFlip.Lexicon.App.Bsky.Richtext;
using FishyFlip.Models;

namespace UniSky.ViewModels.Text;

public class FacetInline
{
    public string Text { get; }
    public IList<FacetProperty> Properties { get; }

    public FacetInline(string text, FacetProperty[] facetTypes = null)
    {
        Text = text;
        Properties = facetTypes ?? [];
    }
}

public abstract class FacetProperty { }

public class LinkProperty(string url) : FacetProperty
{
    public string Url { get; } = url;
}

public class MentionProperty(ATDid did) : FacetProperty
{
    public ATDid Did { get; } = did;
}

public class TagProperty(string tag) : FacetProperty
{
    public string Tag { get; } = tag;
}


// TODO: BluemojiFacet
//       FormattedFacet

public class RichTextViewModel
{
    private static readonly Encoding UTF8NoBom
        = new UTF8Encoding(false);

    private readonly string text;
    private readonly byte[] utf8Text;
    private readonly Facet[] facets;

    public IList<FacetInline> Facets { get; }

    public RichTextViewModel(string text, IList<Facet> facets)
    {
        this.text = text;
        this.utf8Text = UTF8NoBom.GetBytes(text);
        this.facets = [.. facets];

        this.Facets = ParseFacets();
    }

    private IList<FacetInline> ParseFacets()
    {
        var facetInlines = new List<FacetInline>(facets.Length + 5);

        var idx = 0L;
        var utf8Span = new Span<byte>(utf8Text);
        for (int i = 0; i < facets.Length; i++)
        {
            var facet = facets[i];
            var start = facet.Index!.ByteStart.Value;
            var end = facet.Index!.ByteEnd.Value;

            // we have some leading text
            if (idx < start)
            {
                facetInlines.Add(new FacetInline(GetString(utf8Span, idx, start - 1)));
            }

            var str = GetString(utf8Span, start, end - 1);
            var facetTypes = facet.Features
                .Select(s => s switch
                {
                    Link l => new LinkProperty(l.Uri!),
                    Tag t => new TagProperty(t.TagValue!),
                    Mention m => new MentionProperty(m.Did!),
                    _ => (FacetProperty)null
                })
                .Where(f => f is not null)
                .ToArray();

            facetInlines.Add(new FacetInline(str, facetTypes));

            idx = end;
        }

        if (idx < utf8Span.Length)
        {
            facetInlines.Add(new FacetInline(GetString(utf8Span, idx, utf8Span.Length - 1)));
        }

        return facetInlines;
    }

    private unsafe string GetString(Span<byte> utf8Text, long start, long end)
    {
        var span = utf8Text.Slice((int)start, (int)Math.Min((end - start + 1), utf8Text.Length - start));
        fixed (byte* ptr = span)
        {
            return UTF8NoBom.GetString(ptr, span.Length);
        }
    }
}
