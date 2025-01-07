using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Feed;

namespace UniSky.Helpers;

public class UrlHelpers
{
    private const string BSKY_APP = "https://bsky.app";
    private const string PROFILES_ROUTE = "/profile";
    private const string POST_ROUTE = "/post";

    private const string INAVLID_HANDLE = "handle.invalid";

    public static string GetPostURL(PostView postView)
    {
        return string.Concat(
            BSKY_APP,
            PROFILES_ROUTE,
            "/",
            GetHandleSegment(postView.Author!),
            POST_ROUTE,
            "/",
            postView.Uri.Rkey);
    }

    private static string GetHandleSegment(ProfileViewBasic profileViewBasic)
    {
        if (profileViewBasic.Handle?.Handle is not (null or INAVLID_HANDLE))
            return profileViewBasic.Handle.Handle;

        return profileViewBasic.Did.ToString();
    }
}
