using System;
using System.ComponentModel;
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
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

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

    [ObservableProperty]
    private double maxWidth = double.PositiveInfinity;
    [ObservableProperty]
    private double maxHeight = double.PositiveInfinity;

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
        Images = [.. embed.Images.Select(i => new PostEmbedImageViewModel(this, id, i))];

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
        Images = [.. embed.Images.Select(i => new PostEmbedImageViewModel(this, i))];

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
            MaxWidth = double.PositiveInfinity;
            MaxHeight = double.PositiveInfinity;
        }
        else
        {
            AspectRatio = new AspectRatioConstraint(Images.Length switch
            {
                1 => firstRatio.Width > 640 || firstRatio.Height > 640 ?
                    Math.Max((double)firstRatio.Width / firstRatio.Height, 0.5)
                    : ((double)firstRatio.Width / firstRatio.Height),
                2 => 2.0,
                3 => 2.0,
                4 => 3.0 / 2.0,
                _ => throw new NotImplementedException()
            });

            if (Images.Length == 1)
            {
                MaxWidth = Images[0].MaxWidth + 16;
                MaxHeight = Images[0].MaxHeight + 16;
            }
            else
            {
                MaxWidth = double.PositiveInfinity;
                MaxHeight = double.PositiveInfinity;
            }
        }
    } 

    private void OnImagePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MaxHeight))
            return;

        if (sender is not PostEmbedImageViewModel vm || double.IsInfinity(vm.MaxWidth) || double.IsInfinity(vm.MaxHeight))
            return;

        if (Array.IndexOf(Images, vm) != 0)
            return;

        SetAspectRatio(new AspectRatio((long)vm.MaxWidth, (long)vm.MaxHeight));
    }

    [RelayCommand]
    private async Task ShowImageGalleryAsync(object parameter)
    {
        var settingsService = ServiceContainer.Scoped.GetRequiredService<ITypedSettings>();
        if (parameter is Control control)
            parameter = control.Tag;
        else
            control = null;

        var idx = Array.IndexOf(Images, parameter);
        if (idx == -1)
            idx = 0;

        if (control != null && !settingsService.UseMultipleWindows)
        {
            ConnectedAnimationService.GetForCurrentView()
                .PrepareToAnimate("GalleryView", control);
        }

        var genericOverlay = ServiceContainer.Scoped.GetRequiredService<IStandardOverlayService>();
        await genericOverlay.ShowAsync<GalleryControl>(new ShowGalleryArgs(id, embedView, embed, idx));
    }
}
