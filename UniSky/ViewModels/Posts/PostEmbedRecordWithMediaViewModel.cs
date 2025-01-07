using CommunityToolkit.Mvvm.ComponentModel;
using FishyFlip.Lexicon.App.Bsky.Embed;

namespace UniSky.ViewModels.Posts;

public partial class PostEmbedRecordWithMediaViewModel : PostEmbedViewModel
{
    [ObservableProperty]
    private PostEmbedViewModel record;
    [ObservableProperty]
    private PostEmbedViewModel media;

    public PostEmbedRecordWithMediaViewModel(ViewRecordWithMedia embed, bool isNested) : base(embed)
    {
        Record = !isNested ? PostViewModel.CreateEmbedViewModel(embed.Record, isNested) : null;
        Media = PostViewModel.CreateEmbedViewModel(embed.Media, true);
    }
}
