using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FishyFlip;
using FishyFlip.Lexicon;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Models;
using FishyFlip.Tools;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using UniSky.Extensions;
using UniSky.Helpers.Interop;
using UniSky.Services;
using Windows.Foundation.Metadata;
using Windows.Phone;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace UniSky.ViewModels.Profile;

public partial class ProfilePageViewModel : ProfileViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Followers))]
    private int followerCount;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Following))]
    private int followingCount;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Posts))]
    private int postCount;

    [ObservableProperty]
    private ProfileFeedViewModel selectedFeed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Theme))]
    private bool? isLight;
    [ObservableProperty]
    private Color avatarColor;

    public Visibility ShowBio
        => !string.IsNullOrWhiteSpace(Bio) ? Visibility.Visible : Visibility.Collapsed;

    public ElementTheme Theme
        => IsLight.HasValue ? IsLight.Value ? ElementTheme.Dark : ElementTheme.Light : ElementTheme.Default;

    public string Followers
        => FollowerCount.ToMetric(decimals: 2);
    public string Following
        => FollowingCount.ToMetric(decimals: 2);
    public string Posts
        => PostCount.ToMetric(decimals: 2);

    public ObservableCollection<ProfileFeedViewModel> Feeds { get; }

    public ProfilePageViewModel() : this((ATDid)null) { }

    public ProfilePageViewModel(ATDid did)
    {
        this.id = did ?? ServiceContainer.Scoped.GetRequiredService<IProtocolService>().Protocol.Session?.Did;

        Feeds = [];
        SelectedFeed = null;
        Task.Run(LoadAsync);
    }

    public ProfilePageViewModel(ATObject profile)
        : base(profile)
    {
        var protocol = ServiceContainer.Scoped.GetRequiredService<IProtocolService>();
        if (profile is ProfileViewDetailed detailed)
        {
            Populate(detailed);
        }

        Feeds =
        [
            new ProfileFeedViewModel(this, "posts_no_replies", profile, protocol),
            new ProfileFeedViewModel(this, "posts_with_replies", profile, protocol),
            new ProfileFeedViewModel(this, "posts_with_media", profile, protocol)
        ];

        SelectedFeed = Feeds[0];

        Task.Run(LoadAsync);
    }

    private async Task LoadAsync()
    {
        using var context = this.GetLoadingContext();

        try
        {
            var protocol = ServiceContainer.Scoped.GetRequiredService<IProtocolService>();
            var profile = (await protocol.Protocol.GetProfileAsync(this.id).ConfigureAwait(false))
                .HandleResult();

            syncContext.Post(() =>
            {
                if (Feeds.Count == 0)
                {
                    Feeds.Add(new ProfileFeedViewModel(this, "posts_no_replies", profile, protocol));
                    Feeds.Add(new ProfileFeedViewModel(this, "posts_with_replies", profile, protocol));
                    Feeds.Add(new ProfileFeedViewModel(this, "posts_with_media", profile, protocol));

                    SelectedFeed = Feeds[0];
                }

                Populate(profile);
            });

            await CalculateLightnessAsync(profile)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.SetErrored(ex);
        }
    }

    private void Populate(ProfileViewDetailed profile)
    {
        base.Populate(profile);

        this.FollowerCount = (int)profile.FollowersCount;
        this.FollowingCount = (int)profile.FollowsCount;
        this.PostCount = (int)profile.PostsCount;
    }

    protected override void OnLoadingChanged(bool value)
    {
        if (!ApiInformation.IsApiContractPresent(typeof(PhoneContract).FullName, 1))
            return;

        this.syncContext.Post(() =>
        {
            var statusBar = StatusBar.GetForCurrentView();
            _ = statusBar.ShowAsync();

            statusBar.ProgressIndicator.ProgressValue = null;

            if (value)
            {
                _ = statusBar.ProgressIndicator.ShowAsync();
            }
            else
            {
                _ = statusBar.ProgressIndicator.HideAsync();
            }
        });
    }

    internal void Select(ProfileFeedViewModel profileFeedViewModel)
    {
        SelectedFeed = profileFeedViewModel;
    }

    private async Task CalculateLightnessAsync(ProfileViewDetailed profile)
    {
        var lightness = 0.0f;
        if (string.IsNullOrWhiteSpace(profile.Banner))
            return;

        var randomAccessStreamRef = RandomAccessStreamReference.CreateFromUri(new Uri(profile.Banner));
        using var randomAccessStream = await randomAccessStreamRef.OpenReadAsync()
            .AsTask()
            .ConfigureAwait(false);

        lightness = await BitmapInterop.GetImageAverageBrightnessAsync(randomAccessStream)
            .ConfigureAwait(false);
        IsLight = lightness < 0.55f;
        Debug.WriteLine(lightness);

        syncContext.Post(() =>
        {
            var safeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();
            if (IsLight == true)
            {
                safeAreaService.SetTitlebarTheme(ElementTheme.Dark);
            }
            else
            {
                safeAreaService.SetTitlebarTheme(ElementTheme.Light);
            }
        });
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(Bio))
            this.OnPropertyChanged(nameof(ShowBio));
    }
}
