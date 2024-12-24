using FishyFlip.Lexicon;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Embed;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Models;
using FeedViewerState = FishyFlip.Lexicon.App.Bsky.Feed.ViewerState;

namespace UniSky.Moderation;

public class ModerationSubjectPost : ModerationSubject
{
    public ProfileViewBasic? Author { get; }
    public ATObject? Embed { get; }
    public ATObject? Record { get; }
    public FeedViewerState? Viewer { get; }
    public ATUri Uri { get; set; }

    public ModerationSubjectPost(PostView view) : base(view, view.Labels ?? [])
    {
        Author = view.Author;
        Record = view.Record;
        Embed = view.Embed;
        Viewer = view.Viewer;
        Uri = view.Uri!;
    }

    public ModerationSubjectPost(ViewRecord view, Post post)
         : base(view, view.Labels ?? [])
    {
        Author = view.Author;
        Record = post;
        Embed = post.Embed;
        Uri = view.Uri!;
        Viewer = null;
    }

    public static implicit operator ModerationSubjectPost(PostView view)
        => new ModerationSubjectPost(view);
}
