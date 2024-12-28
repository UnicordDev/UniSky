using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using UniSky.Moderation;
using UniSky.Services;

namespace UniSky.ViewModels.Posts;

public partial class ContentWarningViewModel : ViewModelBase
{
    private readonly IModerationService moderationService
        = ServiceContainer.Default.GetRequiredService<IModerationService>();

    [ObservableProperty]
    private string warning;

    [ObservableProperty]
    private bool isHidden;

    [ObservableProperty]
    private bool canOverride = true;

    public ContentWarningViewModel(ModerationUI mediaFilter)
    {
        var cause = mediaFilter.Blurs.FirstOrDefault();
        if (cause == null)
            return;

        if (cause is LabelModerationCause label)
        {
            if (moderationService.TryGetLocalisedStringsForLabel(label.LabelDef, out var strings))
            {
                Warning = strings.Name;
            }
            else
            {
                Warning = label.LabelDef.Identifier.ToString();
            }
        }
        else
        {
            Warning = "Hidden";
        }

        IsHidden = true;
        CanOverride = !mediaFilter.NoOverride;
    }
}
