using System;
using CommunityToolkit.Mvvm.ComponentModel;
using FishyFlip.Lexicon.App.Bsky.Embed;
using FishyFlip.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI.Controls;
using UniSky.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace UniSky.ViewModels.Posts;

public partial class PostEmbedImageViewModel : ViewModelBase
{
    // how far a single carousel item is allowed to deviate from square before we stop
    // letting it dictate its own width. panoramas would otherwise be metres wide.
    private const double MinCarouselRatio = 0.5;
    private const double MaxCarouselRatio = 2.0;

    private readonly ICdnUrlService urlService
        = ServiceContainer.Scoped.GetService<ICdnUrlService>();

    private readonly PostEmbedImagesViewModel images;

    [ObservableProperty]
    private BitmapImage thumbnailUrl;

    [ObservableProperty]
    private double maxWidth = double.PositiveInfinity;

    [ObservableProperty]
    private double maxHeight = double.PositiveInfinity;

    /// <summary>
    /// This image's own aspect ratio, used to size it within the carousel strip.
    /// </summary>
    [ObservableProperty]
    private AspectRatioConstraint ratio = new(1.0);

    /// <summary>
    /// The embed this image belongs to, so item templates can reach its commands.
    /// </summary>
    public PostEmbedImagesViewModel Parent
        => images;

    private PostEmbedImageViewModel()
    {
        ThumbnailUrl = new BitmapImage() { AutoPlay = false };
        ThumbnailUrl.ImageOpened += OnImageOpened;
    }

    public PostEmbedImageViewModel(PostEmbedImagesViewModel images, ATIdentifier id, Image image) : this()
    {
        this.images = images;
        ThumbnailUrl.UriSource = new Uri(urlService.ProcessCdnUrl($"https://cdn.bsky.app/img/feed_thumbnail/plain/{id}/{image.ImageValue.Ref.Link}"));
        MaxWidth = image.AspectRatio?.Width ?? double.PositiveInfinity;
        MaxHeight = image.AspectRatio?.Height ?? double.PositiveInfinity;
        Ratio = ClampRatio(MaxWidth, MaxHeight);
    }

    public PostEmbedImageViewModel(PostEmbedImagesViewModel images, ViewImage image) : this()
    {
        this.images = images;
        ThumbnailUrl.UriSource = new Uri(urlService.ProcessCdnUrl(image.Thumb));
        MaxWidth = image.AspectRatio?.Width ?? double.PositiveInfinity;
        MaxHeight = image.AspectRatio?.Height ?? double.PositiveInfinity;
        Ratio = ClampRatio(MaxWidth, MaxHeight);
    }

    private void OnImageOpened(object sender, RoutedEventArgs e)
    {
        MaxWidth = ThumbnailUrl.PixelWidth;
        MaxHeight = ThumbnailUrl.PixelHeight;
        Ratio = ClampRatio(MaxWidth, MaxHeight);
    }

    private static AspectRatioConstraint ClampRatio(double width, double height)
    {
        if (double.IsNaN(width) || double.IsNaN(height) ||
            double.IsInfinity(width) || double.IsInfinity(height) ||
            width <= 0 || height <= 0)
            return new AspectRatioConstraint(1.0);

        return new AspectRatioConstraint(Math.Clamp(width / height, MinCarouselRatio, MaxCarouselRatio));
    }
}
