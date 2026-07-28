using FishyFlip.Lexicon;
using UniSky.Services.Navigation;

namespace UniSky.ViewModels.Posts;

public abstract partial class PostEmbedViewModel : ViewModelBase
{
    public PostEmbedViewModel(ATObject embed)
    {
        //Debug.WriteLine(embed?.Type);
    }
    
    public PostEmbedViewModel(INavigationContext navigation, ATObject embed)
        : base(navigation)
    {
    }
}
