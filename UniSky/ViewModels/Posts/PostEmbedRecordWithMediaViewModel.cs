using CommunityToolkit.Mvvm.ComponentModel;
using FishyFlip.Lexicon.App.Bsky.Embed;
using UniSky.Services.Navigation;

namespace UniSky.ViewModels.Posts;

public partial class PostEmbedRecordWithMediaViewModel : PostEmbedViewModel
{
    [ObservableProperty]
    private PostEmbedViewModel record;
    [ObservableProperty]
    private PostEmbedViewModel media;

    public PostEmbedRecordWithMediaViewModel(INavigationContext navigation, ViewRecordWithMedia embed, bool isNested) : base(navigation, embed)
    {
        Record = !isNested ? PostViewModel.CreateEmbedViewModel(navigation, embed.Record, isNested) : null;
        Media = PostViewModel.CreateEmbedViewModel(navigation, embed.Media, true);
    }
}
