using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Io;
using Microsoft.Extensions.Logging;
using UniSky.Helpers.AngleSharp;
using UniSky.Models.Embed;

namespace UniSky.Services;

public class AngleSharpEmbedExtractor : IEmbedExtractor
{
    private readonly ILogger<AngleSharpEmbedExtractor> logger;
    private readonly IConfiguration configuration;
    private readonly HttpClient httpClient
        = new HttpClient();

    public AngleSharpEmbedExtractor(ILogger<AngleSharpEmbedExtractor> logger)
    {
        this.logger = logger;
        httpClient.DefaultRequestHeaders.Add("User-Agent", Constants.CrawlerUserAgent);

        configuration = Configuration.Default
            .WithCulture(CultureInfo.CurrentCulture)
            .WithRequester(new CustomHttpClientRequester(httpClient))
            .WithDefaultLoader(new LoaderOptions() { IsNavigationDisabled = false })
            .WithTemporaryCookies();
    }

    public async Task<UriEmbedDetails?> ExtractEmbedAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        if (uri.Scheme.ToLowerInvariant() is not ("http" or "https"))
            return null;

        var browsingContext = BrowsingContext.New(configuration);
        var document = await browsingContext.OpenAsync(new Url(uri.ToString()), cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (document.StatusCode != System.Net.HttpStatusCode.OK)
            return null;

        var title = ExtractTitleFromDocument(document);
        var description = ExtractDescriptionFromDocument(document);
        var imageUrl = ExtractImageFromDocument(document);

        logger.LogDebug($"{title} - {description}, {imageUrl}");
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(description) && imageUrl == null)
            return null;

        return new UriEmbedDetails(title, description, imageUrl);
    }

    private string ExtractTitleFromDocument(IDocument document)
    {
        var titleOpenGraph = document.QuerySelector<IHtmlMetaElement>("meta[property='og:title']");
        if (titleOpenGraph != null)
            return titleOpenGraph.Content;

        return document.Title;
    }

    private string ExtractDescriptionFromDocument(IDocument document)
    {
        var descriptionOpenGraph = document.QuerySelector<IHtmlMetaElement>("meta[property='og:description']");
        if (descriptionOpenGraph != null)
            return descriptionOpenGraph.Content;

        var metaDescription = document.QuerySelector<IHtmlMetaElement>("meta[name='description']");
        if (metaDescription != null)
            return metaDescription.Content;

        return null;
    }

    private UriEmbedImage? ExtractImageFromDocument(IDocument document)
    {
        var imageOpenGraph = document.QuerySelector<IHtmlMetaElement>("meta[property='og:image'], meta[property='og:image:secure_url']");
        if (!string.IsNullOrWhiteSpace(imageOpenGraph?.Content) && Uri.TryCreate(imageOpenGraph.Content, UriKind.RelativeOrAbsolute, out var uri))
        {
            var url = new Url(imageOpenGraph.Content, document.BaseUri);

            var widthElement = document.QuerySelector<IHtmlMetaElement>("meta[property='og:image:width']");
            var heightElement = document.QuerySelector<IHtmlMetaElement>("meta[property='og:image:height']");
            var altElement = document.QuerySelector<IHtmlMetaElement>("meta[property='og:image:alt']");

            int? width = null, height = null;
            if (!string.IsNullOrWhiteSpace(widthElement?.Content) && int.TryParse(widthElement.Content, out var widthInt))
                width = widthInt;
            if (!string.IsNullOrWhiteSpace(heightElement?.Content) && int.TryParse(heightElement.Content, out var heightInt))
                height = heightInt;

            return new UriEmbedImage(url.ToString(), altElement?.Content, width, height);
        }

        return null;
    }
}
