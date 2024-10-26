using System.Diagnostics;
using FishyFlip.Models;

namespace UniSky.ViewModels.Posts;

public abstract partial class PostEmbedViewModel : ViewModelBase
{
    public PostEmbedViewModel(Embed embed)
    {
        Debug.WriteLine(embed?.Type);
    }
}
