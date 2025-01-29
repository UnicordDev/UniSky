using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Models;
using FishyFlip.Tools;
using Microsoft.Extensions.DependencyInjection;
using UniSky.Extensions;
using UniSky.Services;

namespace UniSky.ViewModels.Thread;

public partial class ThreadViewModel : ViewModelBase
{
    private readonly ATUri uri;

    [ObservableProperty]
    private ThreadPostViewModel selected;

    public ObservableCollection<ThreadPostViewModel> Posts { get; }

    public ThreadViewModel(ATUri uri)
    {
        this.uri = uri;
        this.Posts = [];

        Task.Run(LoadAsync);
    }

    private async Task LoadAsync()
    {
        var protocol = ServiceContainer.Scoped.GetRequiredService<IProtocolService>()
            .Protocol;

        using var loading = this.GetLoadingContext();

        try
        {
            var thread = (await protocol.GetPostThreadAsync(this.uri)
                .ConfigureAwait(false))
                .HandleResult();

            //if (thread.Thread is BlockedPost or NotFoundPost)
            if (thread.Thread is not ThreadViewPost tvp)
            {
                // TODO: handle this
                return;
            }

            static IEnumerable<ThreadViewPost> GetParents(ThreadViewPost post)
            {
                if (post.Parent is not ThreadViewPost { } parent)
                    yield break;

                foreach (var item in GetParents(parent))
                    yield return item;

                yield return parent;
            }

            static IEnumerable<ThreadViewPost> GetChildren(ThreadViewPost post)
            {
                var topReply = post.Replies?.FirstOrDefault();
                if (topReply is not ThreadViewPost { } topReplyPost)
                    yield break;

                yield return topReplyPost;

                foreach (var reply in GetChildren(topReplyPost))
                    yield return reply;
            }

            var replies = tvp.Replies
                .OfType<ThreadViewPost>()
                .OrderByDescending(p => p.Post?.Author?.Did.ToString() == tvp.Post.Author?.Did.ToString())
                .Select(s => (ThreadViewPost[])[s, .. GetChildren(s)])
                .ToList();

            syncContext.Post(() =>
            {
                foreach (var item in GetParents(tvp))
                    Posts.Add(new ThreadPostViewModel(item) { HasChild = true });

                Posts.Add(Selected = new ThreadPostViewModel(tvp, true));

                foreach (var list in replies)
                {
                    for (int i = 0; i < list.Length; i++)
                    {
                        Posts.Add(new ThreadPostViewModel(list[i])
                        {
                            HasChild = list.Length > 1 && (i < (list.Length - 1))
                        });
                    }
                }
            });
        }
        catch (Exception ex)
        {
            this.SetErrored(ex);
        }
    }
}
