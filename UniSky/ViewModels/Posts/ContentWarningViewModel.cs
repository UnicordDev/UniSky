using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using FishyFlip.Models;
using UniSky.Moderation;
using UniSky.Services;

namespace UniSky.ViewModels.Posts;

public partial class ContentWarningViewModel : ViewModelBase
{
    private readonly IModerationService moderationService
        = ServiceContainer.Default.GetRequiredService<IModerationService>();
    private readonly IContentRevealService revealService
        = ServiceContainer.Default.GetRequiredService<IContentRevealService>();

    private readonly ATUri uri;

    [ObservableProperty]
    private string warning;

    [ObservableProperty]
    private string appliedBy;

    [ObservableProperty]
    private bool isHidden;

    [ObservableProperty]
    private bool canOverride = true;
    
    public ContentWarningViewModel(ModerationUI mediaFilter, ATUri uri = null)
    {
        this.uri = uri;

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

            if (moderationService.TryGetDisplayNameForLabeler(label.LabelDef, out var displayName))
            {
                AppliedBy = $"Applied by {displayName}";
            }
        }
        else
        {
            Warning = "Hidden";
        }

        CanOverride = !mediaFilter.NoOverride;
    
        // TODO: sync event
        IsHidden = !revealService.IsRevealed(uri);
    }
    
    partial void OnIsHiddenChanged(bool value)
        => revealService.SetRevealed(uri, !value);
}
