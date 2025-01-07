using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FishyFlip.Lexicon;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Embed;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Lexicon.Com.Atproto.Repo;
using FishyFlip.Models;
using FishyFlip.Tools;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using UniSky.Controls.Compose;
using UniSky.Helpers;
using UniSky.Moderation;
using UniSky.Pages;
using UniSky.Services;
using UniSky.ViewModels.Moderation;
using UniSky.ViewModels.Profile;
using UniSky.ViewModels.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;

namespace UniSky.ViewModels.Posts;

public partial class PostViewModel : ViewModelBase
{
    private ATUri like;
    private ATUri repost;

    private readonly IModerationService moderationService
        = ServiceContainer.Default.GetRequiredService<IModerationService>();

    private readonly ITypedSettings settingsService
        = ServiceContainer.Default.GetRequiredService<ITypedSettings>();

    [ObservableProperty]
    private ProfileViewModel author;

    [ObservableProperty]
    private string date;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Likes))]
    private int likeCount;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Retweets))]
    private int retweetCount;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Replies))]
    private int replyCount;

    [ObservableProperty]
    private bool isLiked;
    [ObservableProperty]
    private bool isRetweeted;

    [ObservableProperty]
    private ProfileViewModel retweetedBy;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowReplyContainer))]
    private ProfileViewModel replyTo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEmbed))]
    private PostEmbedViewModel embed;

    [ObservableProperty]
    private bool canReply;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowReplyContainer))]
    [NotifyPropertyChangedFor(nameof(Borders))]
    private bool hasParent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Borders))]
    private bool hasChild;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Text))]
    private RichTextViewModel richText;

    [ObservableProperty]
    private ContentWarningViewModel warning;

    public ATUri Uri { get; }

    public Post Post { get; }
    public PostView View { get; }
    public ModerationDecision Moderation { get; }

    public string Text
        => string.Concat(RichText.Facets.Select(s => s.Text));

    public string Likes
        => ToNumberString(LikeCount);
    public string Retweets
        => ToNumberString(RetweetCount);
    public string Replies
        => ToNumberString(ReplyCount);

    public bool HasEmbed
        => Embed != null;
    public bool ShowReplyContainer
        => ReplyTo != null && !HasParent;
    public bool ShowReplyLine
        => HasChild;
    public Thickness Borders
        => HasChild ? new Thickness() : new Thickness(0, 0, 0, 1);

    public ObservableCollection<LabelViewModel> Labels { get; } = [];

    public PostViewModel(FeedViewPost feedPost, bool hasParent = false)
        : this(feedPost.Post)
    {
        HasParent = hasParent;

        if (feedPost.Reason is ReasonRepost { By: ProfileViewBasic { } by })
        {
            RetweetedBy = new ProfileViewModel(by);
        }

        if (feedPost.Reply is ReplyRef { Parent: PostView { Author: ProfileViewBasic { } author } })
        {
            ReplyTo = new ProfileViewModel(author);
        }

        if (feedPost.FeedContext != null && settingsService.ShowFeedContext)
        {
            var strings = ResourceLoader.GetForCurrentView();
            Labels.Insert(0, new LabelViewModel()
            {
                Name = feedPost.FeedContext,
                Description = strings.GetString("FeedContextLabel_Description"),
                AppliedBy = strings.GetString("FeedContextLabel_AppliedBy")
            });
        }
    }

    public PostViewModel(PostView view, bool hasChild = false)
    {
        if (view.Record is not Post post)
            throw new InvalidOperationException();

        this.View = view;
        this.Post = post;
        this.Uri = view.Uri;

        var moderator = new Moderator(moderationService.ModerationOptions);
        Moderation = moderator.ModeratePost(view);

        HasChild = hasChild;

        RichText = new RichTextViewModel(post.Text, post.Facets ?? []);
        Author = new ProfileViewModel(view.Author);

        var media = Moderation.GetUI(ModerationContext.ContentMedia);
        if (media.Blur)
        {
            Warning = new ContentWarningViewModel(media);
        }

        Embed = CreateEmbedViewModel(view.Embed, false);

        var timeSinceIndex = DateTime.Now - (view.IndexedAt.Value.ToLocalTime());
        var date = timeSinceIndex.Humanize(1, minUnit: Humanizer.Localisation.TimeUnit.Second);
        Date = date;

        LikeCount = (int)(view.LikeCount ?? 0);
        RetweetCount = (int)(view.RepostCount ?? 0);
        ReplyCount = (int)(view.ReplyCount ?? 0);

        if (view.Viewer is not null)
        {
            if (view.Viewer.Like is { } like)
            {
                this.like = like;
                IsLiked = true;
            }

            if (view.Viewer.Repost is { } repost)
            {
                this.repost = repost;
                IsRetweeted = true;
            }

            CanReply = view.Viewer.ReplyDisabled != true;
        }

        foreach (var label in Author.Labels)
            Labels.Add(label);
    }

    [RelayCommand]
    private async Task LikeAsync()
    {
        var protocol = ServiceContainer.Scoped.GetRequiredService<IProtocolService>()
            .Protocol;

        if (IsLiked)
        {
            var like = Interlocked.Exchange(ref this.like, null); // not stressed about threading here, just cleaner way to exchange values
            if (like == null)
                return;

            IsLiked = false;
            LikeCount--;

            _ = (await protocol.Feed.DeleteLikeAsync(like.Rkey).ConfigureAwait(false))
                .HandleResult();
        }
        else
        {
            IsLiked = true;
            LikeCount++;

            this.like = (await protocol.CreateLikeAsync(new StrongRef(View.Uri, View.Cid)).ConfigureAwait(false))
                .HandleResult()?.Uri;
        }
    }

    [RelayCommand(CanExecute = nameof(CanReply))]
    private async Task ReplyAsync()
    {
        var service = ServiceContainer.Scoped.GetRequiredService<ISheetService>();
        await service.ShowAsync<ComposeSheet>(this);
    }

    [RelayCommand]
    private void CopyLink()
    {
        var url = UrlHelpers.GetPostURL(this.View);
        var uri = new Uri(url);

        var attribute = HttpUtility.HtmlAttributeEncode(url);
        var escaped = HttpUtility.HtmlEncode(url);

        var package = new DataPackage();
        package.SetWebLink(uri);
        package.SetText(url);
        package.SetHtmlFormat($"<a href=\"{attribute}\">{escaped}</a>");

        Clipboard.SetContent(package);
    }

    [RelayCommand]
    private void CopyText()
    {
        var package = new DataPackage();
        package.SetText(this.Post.Text);

        // TODO: parse facets to HTML
        // package.SetHtmlFormat(this.post.Text); 

        // TODO: parse facets to RTF
        // package.SetRtf(this.post.Text);

        Clipboard.SetContent(package);
    }

    [RelayCommand]
    private void Share()
    {
        void OnDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var resources = ResourceLoader.GetForViewIndependentUse();

            var url = UrlHelpers.GetPostURL(this.View);
            var uri = new Uri(url);

            var attribute = HttpUtility.HtmlAttributeEncode(url);
            var escaped = HttpUtility.HtmlEncode(url);

            var request = args.Request;
            request.Data.Properties.Title = string.Format(resources.GetString("Share_Username"), Author.Handle);

            request.Data.SetText(Post.Text);
            request.Data.SetWebLink(uri);
            request.Data.SetHtmlFormat($"<a href=\"{attribute}\">{escaped}</a>");
        }

        var manager = DataTransferManager.GetForCurrentView();
        manager.DataRequested += OnDataRequested;

        DataTransferManager.ShowShareUI();
    }

    [RelayCommand]
    private void OpenThread()
    {
        var navigationService = ServiceContainer.Scoped.GetRequiredService<INavigationServiceLocator>()
            .GetNavigationService("Home");
        navigationService.Navigate<ThreadPage>(this.Uri);
    }

    private string ToNumberString(int n)
    {
        if (n == 0)
            return "\xA0";

        return n.ToMetric(decimals: 2);
    }

    internal static PostEmbedViewModel CreateEmbedViewModel(ATObject embed, bool isNested = false)
    {
        if (embed is null)
            return null;

        Debug.WriteLine(embed.GetType());

        return embed switch
        {
            ViewImages images => new PostEmbedImagesViewModel(images),
            ViewVideo video => new PostEmbedVideoViewModel(video),
            ViewExternal external => new PostEmbedExternalViewModel(external),
            ViewRecordWithMedia recordWithMedia => isNested ?
                CreateEmbedViewModel(recordWithMedia.Media, isNested) :
                new PostEmbedRecordWithMediaViewModel(recordWithMedia, isNested),
            ViewRecordDef and { Record: ViewRecord viewRecord } when !isNested => viewRecord.Value switch
            {
                Post post => new PostEmbedPostViewModel(viewRecord, post),
                _ => null
            },
            _ => null
        };
    }
}
