using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Lexicon.Com.Atproto.Repo;
using FishyFlip.Tools;
using Microsoft.Extensions.Logging;
using UniSky.Extensions;
using UniSky.Services;
using UniSky.ViewModels.Error;
using UniSky.ViewModels.Posts;

namespace UniSky.ViewModels.Compose;

public partial class ComposeViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanPost))]
    [NotifyPropertyChangedFor(nameof(Characters))]
    private string _text;
    [ObservableProperty]
    private string _avatarUrl;
    [ObservableProperty]
    private int maxCharacters;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasReply))]
    private PostViewModel replyTo;

    private readonly IProtocolService protocolService;
    private readonly ILogger<ComposeViewModel> logger;

    // TODO: this but better
    public bool IsDirty
        => !string.IsNullOrEmpty(Text);
    // TODO: ditto
    public bool CanPost
        => !string.IsNullOrEmpty(Text) && Text.Length <= 300;
    public int Characters
        => Text?.Length ?? 0;

    public bool HasReply
        => ReplyTo != null;

    public ComposeViewModel(IProtocolService protocolService,
                            ILogger<ComposeViewModel> logger,
                            PostViewModel replyTo = null)
    {
        this.protocolService = protocolService;
        this.logger = logger;

        this.ReplyTo = replyTo;
        this.MaxCharacters = 300;

        Task.Run(LoadAsync);
    }

    [RelayCommand]
    private async Task PostAsync()
    {
        Error = null;
        using var ctx = this.GetLoadingContext();

        try
        {
            var text = Text;
            var replyRef = await GetReplyDefAsync().ConfigureAwait(false);

            var postModel = new Post(text, reply: replyRef);
            var post = (await protocolService.Protocol.CreatePostAsync(postModel)
                .ConfigureAwait(false))
                .HandleResult();

            Text = null;
            syncContext.Post(async () => { await Hide(); });
        }
        catch (Exception ex)
        {
            this.SetErrored(ex);
        }
    }

    private async Task<ReplyRefDef> GetReplyDefAsync()
    {
        ReplyRefDef replyRef = null;
        if (ReplyTo == null)
            return replyRef;

        var replyPost = ReplyTo.View;
        var replyRecord = (await protocolService.Protocol.GetRecordAsync(replyPost.Uri.Did, replyPost.Uri.Collection, replyPost.Uri.Rkey, replyPost.Cid)
            .ConfigureAwait(false))
            .HandleResult();

        if (replyRecord.Value is not Post replyPostFetched)
            throw new InvalidOperationException("Trying to reply to something that isn't a post?");

        var replyPostReplyDef = replyPostFetched.Reply;

        replyRef = new ReplyRefDef()
        {
            Root = replyPostReplyDef?.Root ?? new StrongRef() { Uri = replyPost.Uri, Cid = replyPost.Cid },
            Parent = new StrongRef() { Uri = replyPost.Uri, Cid = replyPost.Cid }
        };

        return replyRef;
    }

    [RelayCommand]
    private async Task Hide()
    {
        var sheetService = Ioc.Default.GetRequiredService<ISheetService>();
        await sheetService.TryCloseAsync();
    }

    private async Task LoadAsync()
    {
        using var loading = this.GetLoadingContext();

        var protocol = protocolService.Protocol;

        try
        {
            var profile = (await protocol.GetProfileAsync(protocol.AuthSession.Session.Did)
                .ConfigureAwait(false))
                .HandleResult();

            AvatarUrl = profile.Avatar;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch user info!");
            this.SetErrored(ex);
        }
    }
}
