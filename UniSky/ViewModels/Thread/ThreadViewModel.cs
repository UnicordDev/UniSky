using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
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

            if (thread.Thread is BlockedPost or NotFoundPost)
            {
                // TODO: handle this
                return;
            }

            static IEnumerable<ThreadViewPost> GetParents(ThreadViewPost post)
            {
                if (post.Parent is not ThreadViewPost parent)
                    yield break;

                foreach (var item in GetParents(parent))
                    yield return item;

                yield return parent;
            }

            var threadView = (ThreadViewPost)thread.Thread;
            var parents = GetParents(threadView).ToList();
            var replies = threadView.Replies
                .OfType<ThreadViewPost>()
                .OrderByDescending(p => p.Post?.Author?.Did.ToString() == threadView.Post.Author?.Did.ToString())
                .ToList();

            syncContext.Post(() =>
            {
                foreach (var item in parents)
                    Posts.Add(new ThreadPostViewModel(item) { HasChild = true });

                var primaryVm = new ThreadPostViewModel(threadView, true);
                Posts.Add(primaryVm);

                foreach (var item in replies)
                    Posts.Add(new ThreadPostViewModel(item));

                Selected = primaryVm;
            });
        }
        catch (Exception ex)
        {
            this.SetErrored(ex);
        }
    }
}
