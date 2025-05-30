﻿using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FishyFlip.Lexicon.App.Bsky.Embed;
using FishyFlip.Models;
using UniSky.Services.Overlay;
using Windows.Foundation;

namespace UniSky.ViewModels.Gallery;

public record ShowGalleryArgs(ATIdentifier Identifier = null,
                              ViewImages ViewImages = null,
                              EmbedImages EmbedImages = null,
                              int Index = 0) : IOverlaySizeProvider
{
    public Size? GetDesiredSize()
    {
        switch (this)
        {
            case { ViewImages.Images: { } images }:
                {
                    var selected = images[Index];
                    if (selected.AspectRatio == null)
                        return null;

                    return new Size(selected.AspectRatio.Width, selected.AspectRatio.Height);
                }

            case { EmbedImages.Images: { } embedImages }:
                {
                    var selected = embedImages[Index];
                    if (selected.AspectRatio == null)
                        return null;

                    return new Size(selected.AspectRatio.Width, selected.AspectRatio.Height);
                }

            default:
                throw new InvalidOperationException("At least one of ViewImages/EmbedImages must be specified");
        }
    }
}

public partial class GalleryImageViewModel : ViewModelBase
{
    [ObservableProperty]
    private string imageUrl;
    [ObservableProperty]
    private string placeholderUrl;
    public GalleryImageViewModel(ViewImage image)
    {
        ImageUrl = image.Fullsize;
        PlaceholderUrl = image.Thumb;
    }

    public GalleryImageViewModel(ATIdentifier id, Image image)
    {
        // TODO: this
        ImageUrl = $"https://cdn.bsky.app/img/feed_fullsize/plain/{id}/{image.ImageValue.Ref.Link}@jpeg";
        PlaceholderUrl = ImageUrl;
    }
}

public partial class GalleryViewModel : ViewModelBase
{
    [ObservableProperty]
    private int selectedIndex;

    public ObservableCollection<GalleryImageViewModel> Images { get; } = [];

    public GalleryViewModel(ShowGalleryArgs args)
    {
        switch (args)
        {
            case { ViewImages.Images: { } images }:
                {
                    foreach (var image in images)
                    {
                        Images.Add(new GalleryImageViewModel(image));
                    }

                    break;
                }

            case { EmbedImages.Images: { } embedImages, Identifier: { } id }:
                {
                    foreach (var image in embedImages)
                    {
                        Images.Add(new GalleryImageViewModel(id, image));
                    }

                    break;
                }

            default:
                throw new InvalidOperationException("At least one of ViewImages/EmbedImages must be specified");
        }

        SelectedIndex = Math.Clamp(args.Index, 0, Images.Count - 1);
    }
}
