using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using UniSky.Moderation;
using UniSky.Services;

namespace UniSky.ViewModels.Moderation;

public partial class LabelViewModel : ViewModelBase
{
    [ObservableProperty]
    private string name;
    [ObservableProperty]
    private string description;
    [ObservableProperty]
    private string appliedBy;

    public LabelViewModel(LabelModerationCause label)
    {
        var moderationService = ServiceContainer.Scoped.GetRequiredService<IModerationService>();
        if (moderationService.TryGetLocalisedStringsForLabel(label.LabelDef, out var strings))
        {
            Name = strings.Name;
            Description = strings.Description;
        }
        else
        {
            Name = label.Label.Val;
        }

        if (label.Source.Type == ModerationCauseSourceType.User)
        {
            appliedBy = "author";
        }
        else if (moderationService.TryGetDisplayNameForLabeler(label.LabelDef, out var displayName))
        {
            AppliedBy = displayName;
        }
    }
}
