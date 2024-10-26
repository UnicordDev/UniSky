using CommunityToolkit.Mvvm.ComponentModel;
using FishyFlip.Models;

namespace UniSky.ViewModels.Posts;

public partial class PostEmbedImageViewModel : ViewModelBase
{
    [ObservableProperty]
    private string thumbnailUrl;

    public PostEmbedImageViewModel(ImageView image)
    {
        ThumbnailUrl = image.Thumb;
    }
}
