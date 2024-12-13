using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FishyFlip.Lexicon.App.Bsky.Embed;
using UniSky.Services.Overlay;
using Windows.Foundation;

namespace UniSky.ViewModels.Gallery;

public record ShowGalleryArgs(ViewImages ViewImages = null, EmbedImages EmbedImages = null, int Index = 0) : IOverlaySizeProvider
{
    public Size? GetDesiredSize()
    {
        if (this is { ViewImages.Images: { } images })
        {
            var maxSize = images.Where(m => m.AspectRatio != null)
                                .OrderByDescending(m => m.AspectRatio.Width * m.AspectRatio.Height)
                                .FirstOrDefault();

            if (maxSize == null)
                return null;

            return new Size(maxSize.AspectRatio.Width.Value, maxSize.AspectRatio.Height.Value);
        }
        else if (this is { EmbedImages.Images: { } embedImages })
        {
            var maxSize = embedImages.Where(m => m.AspectRatio != null)
                                     .OrderByDescending(m => m.AspectRatio.Width * m.AspectRatio.Height)
                                     .FirstOrDefault();

            if (maxSize == null)
                return null;

            return new Size(maxSize.AspectRatio.Width.Value, maxSize.AspectRatio.Height.Value);
        }
        else
        {
            throw new InvalidOperationException("At least one of ViewImages/EmbedImages must be specified");
        }
    }
}

public partial class GalleryImageViewModel : ViewModelBase
{
    [ObservableProperty]
    private string imageUrl;

    public GalleryImageViewModel(ViewImage image)
    {
        ImageUrl = image.Fullsize;
    }

    public GalleryImageViewModel(Image image)
    {
        // TODO: this 
    }
}

public partial class GalleryViewModel : ViewModelBase
{
    [ObservableProperty]
    private int selectedIndex;

    public ObservableCollection<GalleryImageViewModel> Images { get; } = [];

    public GalleryViewModel(ShowGalleryArgs args)
    {
        if (args is { ViewImages.Images: { } images })
        {
            foreach (var image in images)
            {
                Images.Add(new GalleryImageViewModel(image));
            }
        }
        else if (args is { EmbedImages.Images: { } embedImages })
        {
            foreach (var image in embedImages)
            {
                Images.Add(new GalleryImageViewModel(image));
            }
        }
        else
        {
            throw new InvalidOperationException("At least one of ViewImages/EmbedImages must be specified");
        }

        SelectedIndex = Math.Clamp(args.Index, 0, Images.Count - 1);
    }
}
