using System;
using CommunityToolkit.Mvvm.ComponentModel;
using FishyFlip.Lexicon.App.Bsky.Embed;
using FishyFlip.Models;
using Microsoft.Extensions.DependencyInjection;
using UniSky.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace UniSky.ViewModels.Posts;

public partial class PostEmbedImageViewModel : ViewModelBase
{
    private readonly ICdnUrlService urlService
        = ServiceContainer.Scoped.GetService<ICdnUrlService>();

    private readonly PostEmbedImagesViewModel images;

    [ObservableProperty]
    private BitmapImage thumbnailUrl;

    [ObservableProperty]
    private double maxWidth = double.PositiveInfinity;

    [ObservableProperty]
    private double maxHeight = double.PositiveInfinity;

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
    }

    public PostEmbedImageViewModel(PostEmbedImagesViewModel images, ViewImage image) : this()
    {
        this.images = images;
        ThumbnailUrl.UriSource = new Uri(urlService.ProcessCdnUrl(image.Thumb));
        MaxWidth = image.AspectRatio?.Width ?? double.PositiveInfinity;
        MaxHeight = image.AspectRatio?.Height ?? double.PositiveInfinity;
    }

    private void OnImageOpened(object sender, RoutedEventArgs e)
    {
        MaxWidth = ThumbnailUrl.PixelWidth;
        MaxHeight = ThumbnailUrl.PixelHeight;
    }
}
