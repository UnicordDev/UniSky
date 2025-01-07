using FishyFlip.Lexicon;

namespace UniSky.ViewModels.Posts;

public abstract partial class PostEmbedViewModel : ViewModelBase
{
    public PostEmbedViewModel(ATObject embed)
    {
        //Debug.WriteLine(embed?.Type);
    }
}
