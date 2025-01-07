using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FishyFlip.Lexicon.App.Bsky.Embed;
using Windows.System;
using Windows.UI.ViewManagement;

namespace UniSky.ViewModels.Posts;

public partial class PostEmbedExternalViewModel : PostEmbedViewModel
{
    private readonly Uri link;

    [ObservableProperty]
    private string title;
    [ObservableProperty]
    private string description;
    [ObservableProperty]
    private string thumbnailUrl;
    [ObservableProperty]
    private string source;

    public PostEmbedExternalViewModel(ViewExternal embed) : base(embed)
    {
        if (embed.External is { } external)
        {
            link = new Uri(external.Uri);

            Title = external.Title;
            Description = external.Description;
            ThumbnailUrl = external.Thumb ?? "";
            Source = link.Host;
        }
    }

    [RelayCommand]
    private async Task OpenLinkAsync()
    {
        await Launcher.LaunchUriAsync(link, new LauncherOptions() { DesiredRemainingView = ViewSizePreference.UseLess });
    }
}
