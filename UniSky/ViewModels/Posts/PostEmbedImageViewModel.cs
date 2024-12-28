using CommunityToolkit.Mvvm.ComponentModel;
using FishyFlip.Lexicon.App.Bsky.Embed;
using FishyFlip.Models;

namespace UniSky.ViewModels.Posts;

public partial class PostEmbedImageViewModel : ViewModelBase
{
    private readonly PostEmbedImagesViewModel images;

    [ObservableProperty]
    private string thumbnailUrl;

    public PostEmbedImageViewModel(PostEmbedImagesViewModel images, ATIdentifier id, Image image)
    {
        this.images = images;
        ThumbnailUrl = $"https://cdn.bsky.app/img/feed_thumbnail/plain/{id}/{image.ImageValue.Ref.Link}@jpeg";
    }

    public PostEmbedImageViewModel(PostEmbedImagesViewModel images, ViewImage image)
    {
        this.images = images;
        ThumbnailUrl = image.Thumb;
    }
}
