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
using Microsoft.Toolkit.Uwp.UI.Extensions;
using UniSky.Controls.Gallery;
using UniSky.Services;
using UniSky.Services.Overlay;
using UniSky.ViewModels.Gallery;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace UniSky.ViewModels.Posts;

public partial class PostEmbedImagesViewModel : PostEmbedViewModel
{
    /// <summary>
    /// The most images the fixed 2x2 grid layout can display, which is also the
    /// <c>app.bsky.embed.images</c> lexicon's own limit.
    /// </summary>
    public const int GridImages = 4;

    /// <summary>
    /// The schema ceiling on <c>app.bsky.embed.gallery</c> items. Note the lexicon separately
    /// documents a soft limit of 10 for authoring UIs.
    /// </summary>
    public const int MaxGalleryImages = 20;

    // how tall the carousel strip is relative to the post column. tuning knob.
    private const double CarouselRatio = 2.0;

    // how much of a viewport a chevron click moves, leaving a sliver of context behind.
    private const double CarouselStep = 0.9;

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

    /// <summary>
    /// Render as a horizontally scrolling strip rather than the grid. Set for gallery embeds, which
    /// carousel at any item count; an <c>app.bsky.embed.images</c> embed never does, since it can
    /// only ever hold <see cref="GridImages"/> images.
    /// </summary>
    public bool IsCarousel { get; }

    public PostEmbedImagesViewModel(ATIdentifier id, EmbedImages embed) : base(embed)
    {
        this.id = id;
        this.embed = embed;

        Count = embed.Images.Count;
        Images = [.. embed.Images.Select(i => new PostEmbedImageViewModel(this, id, i))];

        // this would be problematic
        Debug.Assert(Images.Length > 0 && Images.Length <= MaxGalleryImages);
        Debug.Assert(embed.Images.Count == Images.Length);
        Debug.Assert(Images.Length == Count);

        var firstRatio = embed.Images[0].AspectRatio;
        SetAspectRatio(firstRatio);

        foreach (var image in Images)
        {
            image.PropertyChanged += OnImagePropertyChanged;
        }
    }

    /// <param name="isCarousel">
    /// Set by the <c>app.bsky.embed.gallery</c> adapter, which synthesises a <see cref="ViewImages"/>
    /// and wants the scrolling strip regardless of how many items it holds.
    /// </param>
    public PostEmbedImagesViewModel(ViewImages embed, bool isCarousel = false) : base(embed)
    {
        this.embedView = embed;
        this.IsCarousel = isCarousel;
        Count = embed.Images.Count;
        Images = [.. embed.Images.Select(i => new PostEmbedImageViewModel(this, i))];

        // this would be problematic
        Debug.Assert(Images.Length > 0 && Images.Length <= MaxGalleryImages);
        Debug.Assert(embed.Images.Count == Images.Length);
        Debug.Assert(Images.Length == Count);

        var firstRatio = embed.Images[0].AspectRatio;
        SetAspectRatio(firstRatio);

        foreach (var image in Images)
        {
            image.PropertyChanged += OnImagePropertyChanged;
        }
    }

    private void SetAspectRatio(AspectRatio firstRatio)
    {
        if (IsCarousel)
        {
            // a fixed height strip; each image sizes itself along it from its own ratio
            AspectRatio = new AspectRatioConstraint(CarouselRatio);
            MaxWidth = double.PositiveInfinity;
            MaxHeight = double.PositiveInfinity;
        }
        else if (Images.Length == 1 && firstRatio == null)
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
                _ => CarouselRatio
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
    private void ScrollCarouselBack(ListView list)
        => ScrollCarousel(list, -1);

    [RelayCommand]
    private void ScrollCarouselForward(ListView list)
        => ScrollCarousel(list, +1);

    private static void ScrollCarousel(ListView list, int direction)
    {
        if (list?.FindDescendant<ScrollViewer>() is not { } scroller)
            return;

        var target = scroller.HorizontalOffset + (direction * scroller.ViewportWidth * CarouselStep);
        scroller.ChangeView(Math.Clamp(target, 0, scroller.ScrollableWidth), null, null);
    }

    [RelayCommand]
    private async Task ShowImageGalleryAsync(object parameter)
    {
        // No connected animation into the gallery. The flip view applies its scroll offset on the
        // compositor thread, so for anything but the first image the UI thread doesn't know where
        // the destination actually is until the scroller reports itself settled. Since
        // ConnectedAnimation.TryStart samples the destination on the UI thread, the animation
        // either flew off to the side or played only once the image had already arrived, and no
        // amount of picking a better moment to start it helped.
        if (parameter is Control control)
            parameter = control.Tag;

        var idx = Array.IndexOf(Images, parameter);
        if (idx == -1)
            idx = 0;

        var genericOverlay = ServiceContainer.Scoped.GetRequiredService<IStandardOverlayService>();
        await genericOverlay.ShowAsync<GalleryControl>(new ShowGalleryArgs(id, embedView, embed, idx));
    }
}
