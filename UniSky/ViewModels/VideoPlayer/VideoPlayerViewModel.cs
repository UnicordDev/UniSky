using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FishyFlip.Lexicon.App.Bsky.Embed;
using FishyFlip.Models;
using UniSky.Services.Overlay;
using UniSky.ViewModels.Posts;
using Windows.Foundation;

namespace UniSky.ViewModels.VideoPlayer;

public record ShowVideoPlayerArgs(ViewVideo ViewVideo = null) : IOverlaySizeProvider
{
    public Size? GetDesiredSize()
    {
        if (ViewVideo.AspectRatio != null)
            return new Size(ViewVideo.AspectRatio.Width.Value, ViewVideo.AspectRatio.Height.Value);

        return null;
    }
}

internal class VideoPlayerViewModel : PostEmbedVideoViewModel
{
    public VideoPlayerViewModel(ShowVideoPlayerArgs video) 
        : base(video.ViewVideo)
    {
    }
}
