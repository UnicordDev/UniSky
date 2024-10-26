using System;
using System.Numerics;
using Microsoft.Toolkit.Uwp.UI.Media.Geometry;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Composition;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

namespace UniSky.Helpers.Composition;

internal static class BirdAnimation
{
    // Duplicated in App.xaml too
    private const string BSKY_LOGO
        = "M13.873 3.77C21.21 9.243 29.103 20.342 32 26.3v15.732c0-.335-.13.043-.41.858-1.512 4.414-7.418 21.642-20.923" +
        " 7.87-7.111-7.252-3.819-14.503 9.125-16.692-7.405 1.252-15.73-.817-18.014-8.93C1.12 22.804 0 8.431 0 6.488 0-3.237" +
        " 8.579-.18 13.873 3.77ZM50.127 3.77C42.79 9.243 34.897 20.342 32 26.3v15.732c0-.335.13.043.41.858 1.512 4.414 7.418" +
        " 21.642 20.923 7.87 7.111-7.252 3.819-14.503-9.125-16.692 7.405 1.252 15.73-.817 18.014-8.93C62.88 22.804 64 8.431" +
        " 64 6.488 64-3.237 55.422-.18 50.127 3.77Z";

    private const int WIDTH = 64;
    private const int HEIGHT = 57;

    public static void RunBirdAnimation(Frame frame)
    {
        if (!ApiInformation.IsMethodPresent(typeof(Compositor).FullName, "CreateGeometricClip"))
            return;

        var size = new Size(frame.ActualWidth, frame.ActualHeight);
        var frameVisual = ElementCompositionPreview.GetElementVisual(frame);

        var initialScale = (float)(1.5 * Math.Min(size.Width / 620, 1.0));

        var scale = MathF.Max((float)size.Width / 30, (float)size.Height / 12);
        var offsetX = (float)((size.Width / 2.0));
        var offsetY = (float)((size.Height / 2.0));

        var compositor = frameVisual.Compositor;
        var clip = compositor.CreateGeometricClip(BSKY_LOGO);
        clip.ViewBox = compositor.CreateViewBox();
        clip.ViewBox.Size = new Vector2(WIDTH, HEIGHT);
        clip.ViewBox.Stretch = CompositionStretch.None;
        clip.AnchorPoint = new Vector2(0.5f, 0.5f);
        clip.Scale = new Vector2(initialScale, initialScale);
        clip.Offset = new Vector2(offsetX, offsetY);

        frameVisual.AnchorPoint = new Vector2(0.5f);
        frameVisual.Offset = new Vector3((float)(size.Width / 2), (float)(size.Height / 2), 0);
        frameVisual.Clip = clip;

        var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        batch.Completed += (o, ev) =>
        {
            frameVisual.Clip = null;
            frameVisual.AnchorPoint = Vector2.Zero;
            frameVisual.Offset = Vector3.Zero;
        };

        var ease = compositor.CreateCubicBezierEasingFunction(new Vector2(0.7f, 0), new Vector2(0.84f, 0));
        var ease2 = compositor.CreateCubicBezierEasingFunction(new Vector2(0, 0.55f), new Vector2(0.45f, 1));

        var group = compositor.CreateAnimationGroup();

        var scaleAnimation = compositor.CreateVector2KeyFrameAnimation();
        scaleAnimation.InsertKeyFrame(1.0f, new Vector2(scale, scale), ease);
        scaleAnimation.Duration = TimeSpan.FromSeconds(0.15);
        scaleAnimation.Target = "Scale";
        group.Add(scaleAnimation);

        var offsetAnimation = compositor.CreateVector2KeyFrameAnimation();
        offsetAnimation.InsertKeyFrame(1.0f, new Vector2(offsetX, offsetY - (6 * scale)), ease);
        offsetAnimation.Duration = TimeSpan.FromSeconds(0.15);
        offsetAnimation.Target = "Offset";
        group.Add(offsetAnimation);

        var scaleAnimation2 = compositor.CreateVector3KeyFrameAnimation();
        scaleAnimation2.InsertKeyFrame(0.5f, new Vector3(1.1f), ease);
        scaleAnimation2.InsertKeyFrame(1.0f, new Vector3(1.0f), ease2);
        scaleAnimation2.Duration = TimeSpan.FromSeconds(0.3);
        scaleAnimation2.Target = "Scale";

        clip.StartAnimationGroup(group);
        frameVisual.StartAnimation(scaleAnimation2.Target, scaleAnimation2);

        batch.End();
    }
}