using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using FishyFlip.Lexicon;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Embed;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Lexicon.Com.Atproto.Repo;
using FishyFlip.Models;
using FishyFlip.Tools;
using Humanizer;
using UniSky.Controls.Compose;
using UniSky.Helpers;
using UniSky.Pages;
using UniSky.Services;
using UniSky.ViewModels.Profiles;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace UniSky.ViewModels.Posts;

public partial class PostViewModel : ViewModelBase
{
    private readonly PostView view;
    private readonly Post post;

    private ATUri like;
    private ATUri repost;

    [ObservableProperty]
    private string text;
    [ObservableProperty]
    private ProfileViewModel author;

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
    [NotifyPropertyChangedFor(nameof(ShowReplyContainer))]
    private bool hasParent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Borders))]
    private bool hasChild;
    public Thickness Borders
        => HasChild ? new Thickness() : new Thickness(0, 0, 0, 1);

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

    public Post Post => post;
    public PostView View => view;

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
    }

    public PostViewModel(PostView view, bool hasChild = false)
    {
        if (view.Record is not Post post)
            throw new InvalidOperationException();

        this.view = view;
        this.post = post;

        HasChild = hasChild;

        Author = new ProfileViewModel(view.Author);
        Text = post.Text;
        Embed = CreateEmbedViewModel(view.Embed);

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
        }
    }

    [RelayCommand]
    private async Task LikeAsync()
    {
        var protocol = Ioc.Default.GetRequiredService<IProtocolService>()
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

            this.like = (await protocol.CreateLikeAsync(new StrongRef(view.Uri, view.Cid)).ConfigureAwait(false))
                .HandleResult()?.Uri;
        }
    }

    [RelayCommand]
    private void OpenProfile(UIElement element)
    {
        var service = Ioc.Default.GetRequiredService<INavigationServiceLocator>()
            .GetNavigationService("Home");

        service.Navigate<ProfilePage>(this.view.Author, new ContinuumNavigationTransitionInfo() { ExitElement = element });
    }


    [RelayCommand]
    private async Task ReplyAsync()
    {
        var service = Ioc.Default.GetRequiredService<ISheetService>();
        await service.ShowAsync<ComposeSheet>(this);
    }

    [RelayCommand]
    private void CopyLink()
    {
        var url = UrlHelpers.GetPostURL(this.view);
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
        package.SetText(this.post.Text);

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
            var url = UrlHelpers.GetPostURL(this.view);
            var uri = new Uri(url);

            var attribute = HttpUtility.HtmlAttributeEncode(url);
            var escaped = HttpUtility.HtmlEncode(url);

            var request = args.Request;
            request.Data.Properties.Title = $"@{Author.Handle} on BlueSky";

            request.Data.SetText(post.Text);
            request.Data.SetWebLink(uri);
            request.Data.SetHtmlFormat($"<a href=\"{attribute}\">{escaped}</a>");
        }

        var manager = DataTransferManager.GetForCurrentView();
        manager.DataRequested += OnDataRequested;

        DataTransferManager.ShowShareUI();
    }

    private string ToNumberString(int n)
    {
        if (n == 0)
            return "\xA0";

        return n.ToMetric(decimals: 2);
    }

    private PostEmbedViewModel CreateEmbedViewModel(ATObject embed)
    {
        if (embed is null)
            return null;

        Debug.WriteLine(embed.GetType());

        return embed switch
        {
            ViewImages images => new PostEmbedImagesViewModel(images),
            ViewVideo video => new PostEmbedVideoViewModel(video),
            _ => null
        };
    }
}
