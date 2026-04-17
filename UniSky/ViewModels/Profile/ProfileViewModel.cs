using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FishyFlip.Lexicon;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Graph;
using FishyFlip.Models;
using Microsoft.Extensions.DependencyInjection;
using UniSky.Moderation;
using UniSky.Pages;
using UniSky.Services;
using UniSky.ViewModels.Moderation;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace UniSky.ViewModels.Profile;

public partial class ProfileViewModel : ViewModelBase
{
    private readonly IModerationService moderationService
        = ServiceContainer.Default.GetRequiredService<IModerationService>();
    private readonly ITypedSettings settings
        = ServiceContainer.Default.GetRequiredService<ITypedSettings>();
    protected readonly ICdnUrlService urlService
        = ServiceContainer.Scoped.GetService<ICdnUrlService>();

    private static readonly ResourceLoader strings = ResourceLoader.GetForViewIndependentUse();

    protected ATIdentifier id;
    protected ATObject source;

    [ObservableProperty]
    private string name;
    [ObservableProperty]
    private string handle;
    [ObservableProperty]
    private string avatarUrl;
    [ObservableProperty]
    private string bannerUrl;
    [ObservableProperty]
    private string bio;
    [ObservableProperty]
    private string pronouns;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMutual))]
    [NotifyPropertyChangedFor(nameof(FollowButtonText))]
    private bool isFollowing;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMutual))]
    private bool followsYou;

    [ObservableProperty]
    private bool isMe;

    public ObservableCollection<LabelViewModel> Labels { get; }
    public ModerationDecision ModDecision { get; private set; }

    public bool IsMutual
        => IsFollowing && FollowsYou;
    public string FollowButtonText
        => strings.GetString(IsFollowing ? "Profile_Following" : "Profile_Follow");

    public ProfileViewModel()
    {
        this.id = null;
        this.AvatarUrl = "ms-appx:///Assets/Default/Avatar.png";
        this.Name = "Example User";
        this.Handle = "@example.com";
        this.Pronouns = "it/its";
        this.Labels = [];
    }

    public ProfileViewModel(ATObject obj)
    {
        this.source = obj;
        this.Labels = [];

        Populate(obj);
    }

    protected virtual void Populate(ATObject obj)
    {
        Labels.Clear();

        switch (obj)
        {
            case ProfileView view:
                SetData(view.Did,
                        view.Handle,
                        view.DisplayName,
                        view.Avatar,
                        view.Description,
                        view.Viewer,
                        pronouns: view.Pronouns);

                if (moderationService.ModerationOptions != null)
                    ModDecision = new Moderator(moderationService.ModerationOptions).ModerateProfile(view);
                break;
            case ProfileViewBasic profile:
                SetData(profile.Did,
                        profile.Handle,
                        profile.DisplayName,
                        profile.Avatar,
                        viewerState: profile.Viewer,
                        pronouns: profile.Pronouns);

                if (moderationService.ModerationOptions != null)
                    ModDecision = new Moderator(moderationService.ModerationOptions).ModerateProfile(profile);
                break;
            case ProfileViewDetailed detailed:
                SetData(detailed.Did,
                        detailed.Handle,
                        detailed.DisplayName,
                        detailed.Avatar,
                        detailed.Description,
                        detailed.Viewer,
                        detailed.Banner,
                        detailed.Pronouns);

                if (moderationService.ModerationOptions != null)
                    ModDecision = new Moderator(moderationService.ModerationOptions).ModerateProfile(detailed);
                break;
        }

        if (ModDecision != null)
        {
            DoModeration();
        }
    }

    private void DoModeration()
    {
        var ui = ModDecision.GetUI(ModerationContext.ProfileList);
        foreach (var cause in ui.Informs)
        {
            if (cause is LabelModerationCause label)
                Labels.Add(new LabelViewModel(label));
        }

        var avatar = ModDecision.GetUI(ModerationContext.Avatar);
        if (avatar.Blur || avatar.Alert)
            AvatarUrl = null;

        var banner = ModDecision.GetUI(ModerationContext.Banner);
        if (banner.Blur)
            BannerUrl = null;

        var displayName = ModDecision.GetUI(ModerationContext.DisplayName);
        if (displayName.Blur)
            this.Name = this.Handle;

        var blockCause = ModDecision.BlockCause;
        if (blockCause != null)
        {
            if (blockCause.Type is (ModerationCauseType.Blocking or ModerationCauseType.BlockOther))
            {
                this.Name = strings.GetString("Profile_Blocked");
                this.Bio = strings.GetString("Profile_BlockedUser");
                return;
            }

            if (blockCause.Type is (ModerationCauseType.BlockedBy))
            {
                this.Name = strings.GetString("Profile_Blocked");
                this.Bio = strings.GetString("Profile_BlockedByUser");
                return;
            }
        }
    }

    [RelayCommand]
    private void OpenProfile(UIElement element)
    {
        var service = ServiceContainer.Scoped.GetRequiredService<INavigationServiceLocator>()
            .GetNavigationService("Home");

        service.Navigate<ProfilePage>(this.source, new ContinuumNavigationTransitionInfo() { ExitElement = element });
    }

    [RelayCommand]
    private async Task FollowAsync()
    {
        if (IsFollowing || this.id is not ATDid did)
            return;

        var protocol = ServiceContainer.Default.GetRequiredService<IProtocolService>()
            .Protocol;

        var follow = await protocol.CreateFollowAsync(new Follow(did, DateTime.UtcNow))
            .ConfigureAwait(false);

        follow.Switch(
            output => IsFollowing = true,
            SetErrored);
    }

    public virtual void SetData(ATDid id,
                                ATHandle handle,
                                string displayName,
                                string avatar,
                                string bio = "",
                                ViewerState viewerState = null,
                                string banner = "",
                                string pronouns = "")
    {
        var protocol = ServiceContainer.Default.GetRequiredService<IProtocolService>()
            .Protocol;

        this.id = id;

        this.IsMe = protocol.Session.Did.ToString() == id.ToString();
        this.Handle = ProfileHelpers.ConvertHandle(handle);

        if (viewerState is ViewerState viewer)
        {
            this.IsFollowing = viewer.Following != null;
            this.FollowsYou = viewer.FollowedBy != null;
        }

        this.AvatarUrl = urlService.ProcessCdnUrl(avatar);
        this.Name = string.IsNullOrWhiteSpace(displayName) ? ProfileHelpers.ConvertHandle(handle, true) : displayName;
        this.Bio = bio?.Trim() ?? "";
        this.BannerUrl = urlService.ProcessCdnUrl(banner);
        this.Pronouns = pronouns?.Trim() ?? "";

        if (settings.ShowPronounsAsLabel && !string.IsNullOrWhiteSpace(this.Pronouns))
        {
            this.Labels.Add(new LabelViewModel()
            {
                Name = this.Pronouns,
                AppliedBy = strings.GetString("AppName"),
                Description = strings.GetString("PronounsLabelDescription")
            });
        }
    }
}
