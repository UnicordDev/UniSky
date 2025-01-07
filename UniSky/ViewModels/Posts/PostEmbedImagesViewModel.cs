using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FishyFlip.Lexicon.App.Bsky.Embed;
using FishyFlip.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI.Controls;
using UniSky.Controls.Gallery;
using UniSky.Services;
using UniSky.Services.Overlay;
using UniSky.ViewModels.Gallery;

namespace UniSky.ViewModels.Posts;

public partial class PostEmbedImagesViewModel : PostEmbedViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOne), nameof(IsTwo), nameof(IsThree), nameof(IsFour))]
    private int count;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Image1), nameof(Image2), nameof(Image3), nameof(Image4))]
    private PostEmbedImageViewModel[] images;

    [ObservableProperty]
    private AspectRatioConstraint aspectRatio;

    private readonly ATIdentifier id;
    private readonly EmbedImages embed;
    private readonly ViewImages embedView;

    public PostEmbedImageViewModel Image1
        => Images.ElementAtOrDefault(0);
    public PostEmbedImageViewModel Image2
        => Images.ElementAtOrDefault(1);
    public PostEmbedImageViewModel Image3
        => Images.ElementAtOrDefault(2);
    public PostEmbedImageViewModel Image4
        => Images.ElementAtOrDefault(3);

    // i shouldn't need these *grumble grumble*
    public bool IsOne => Count == 1;
    public bool IsTwo => Count == 2;
    public bool IsThree => Count == 3;
    public bool IsFour => Count == 4;

    public PostEmbedImagesViewModel(ATIdentifier id, EmbedImages embed) : base(embed)
    {
        this.id = id;
        this.embed = embed;

        Count = embed.Images.Count;
        Images = embed.Images.Select(i => new PostEmbedImageViewModel(this, id, i)).ToArray();

        // this would be problematic
        Debug.Assert(Images.Length > 0 && Images.Length <= 4);
        Debug.Assert(embed.Images.Count == Images.Length);
        Debug.Assert(Images.Length == Count);

        var firstRatio = embed.Images[0].AspectRatio;
        SetAspectRatio(firstRatio);
    }

    public PostEmbedImagesViewModel(ViewImages embed) : base(embed)
    {
        this.embedView = embed;
        Count = embed.Images.Count;
        Images = embed.Images.Select(i => new PostEmbedImageViewModel(this, i)).ToArray();

        // this would be problematic
        Debug.Assert(Images.Length > 0 && Images.Length <= 4);
        Debug.Assert(embed.Images.Count == Images.Length);
        Debug.Assert(Images.Length == Count);

        var firstRatio = embed.Images[0].AspectRatio;
        SetAspectRatio(firstRatio);
    }

    private void SetAspectRatio(AspectRatio firstRatio)
    {
        if (Images.Length == 1 && firstRatio == null)
        {
            AspectRatio = new();
        }
        else
        {
            AspectRatio = new AspectRatioConstraint(Images.Length switch
            {
                1 => Math.Max((double)firstRatio.Width.Value / firstRatio.Height.Value, 0.75),
                2 => 2.0,
                3 => 2.0,
                4 => 3.0 / 2.0,
                _ => throw new NotImplementedException()
            });
        }
    }

    [RelayCommand]
    private async Task ShowImageGalleryAsync(object parameter)
    {
        var idx = Array.IndexOf(Images, parameter);
        if (idx == -1)
            idx = 0;

        var genericOverlay = ServiceContainer.Scoped.GetRequiredService<IStandardOverlayService>();
        await genericOverlay.ShowAsync<GalleryControl>(new ShowGalleryArgs(id, embedView, embed, idx));
    }
}
