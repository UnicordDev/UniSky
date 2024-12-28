using System;
using System.Numerics;
using Microsoft.Toolkit.Uwp.UI.Animations.Behaviors;
using Microsoft.Toolkit.Uwp.UI.Media.Geometry;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;

namespace UniSky.Helpers.Composition;

internal static class BirdAnimation
{
    public static void RunBirdAnimation(FrameworkElement extendedSplash, FrameworkElement frame)
    {
        // TODO: fallback to a shape visual
        var frameVisual = ElementCompositionPreview.GetElementVisual(frame);
        var splashVisual = ElementCompositionPreview.GetElementVisual(extendedSplash);

        var compositor = frameVisual.Compositor;
        var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        batch.Completed += (o, ev) =>
        {
            frameVisual.Clip = null;
            frameVisual.AnchorPoint = Vector2.Zero;
            frameVisual.Offset = Vector3.Zero;
        };

        var splashHideAnimation = compositor.CreateScalarKeyFrameAnimation();
        splashHideAnimation.InsertKeyFrame(0.0f, 1.0f);
        splashHideAnimation.InsertKeyFrame(1.0f, 0.0f);
        splashHideAnimation.DelayTime = TimeSpan.FromSeconds(0);
        splashHideAnimation.Duration = TimeSpan.FromSeconds(0.15);

        if (ApiInformation.IsMethodPresent(typeof(Compositor).FullName, "CreateGeometricClip"))
        {
            var appLogoPath = (string)Application.Current.Resources["BlueSkyLogoPath"];
            var appLogoWidth = (int)Application.Current.Resources["BlueSkyLogoWidth"];
            var appLogoHeight = (int)Application.Current.Resources["BlueSkyLogoHeight"];

            var size = new Size(frame.ActualWidth, frame.ActualHeight);

            var initialScale = (float)(1.5 * Math.Min(size.Width / 620, 1.0));

            var scale = MathF.Max((float)size.Width / 30, (float)size.Height / 12);
            var offsetX = (float)(size.Width / 2.0);
            var offsetY = (float)(size.Height / 2.0);

            var clip = compositor.CreateGeometricClip(appLogoPath);
            clip.ViewBox = compositor.CreateViewBox();
            clip.ViewBox.Size = new Vector2(appLogoWidth, appLogoHeight);
            clip.ViewBox.Stretch = CompositionStretch.None;
            clip.AnchorPoint = new Vector2(0.5f, 0.5f);
            clip.Scale = new Vector2(initialScale, initialScale);
            clip.Offset = new Vector2(offsetX, offsetY);

            frameVisual.AnchorPoint = new Vector2(0.5f);
            frameVisual.Offset = new Vector3((float)(size.Width / 2), (float)(size.Height / 2), 0);
            frameVisual.Clip = clip;

            var ease = compositor.CreateCubicBezierEasingFunction(new Vector2(0.7f, 0), new Vector2(0.84f, 0));
            var ease2 = compositor.CreateCubicBezierEasingFunction(new Vector2(0, 0.55f), new Vector2(0.45f, 1));
            var group = compositor.CreateAnimationGroup();

            var scaleAnimation = compositor.CreateVector2KeyFrameAnimation();
            scaleAnimation.InsertKeyFrame(1.0f, new Vector2(scale, scale), ease);
            scaleAnimation.DelayTime = TimeSpan.FromSeconds(0.15);
            scaleAnimation.Duration = TimeSpan.FromSeconds(0.15);
            scaleAnimation.Target = "Scale";
            group.Add(scaleAnimation);

            var offsetAnimation = compositor.CreateVector2KeyFrameAnimation();
            offsetAnimation.InsertKeyFrame(1.0f, new Vector2(offsetX, offsetY - (6 * scale)), ease);
            offsetAnimation.DelayTime = TimeSpan.FromSeconds(0.15);
            offsetAnimation.Duration = TimeSpan.FromSeconds(0.15);
            offsetAnimation.Target = "Offset";
            group.Add(offsetAnimation);

            var scaleAnimation2 = compositor.CreateVector3KeyFrameAnimation();
            scaleAnimation2.InsertKeyFrame(0.5f, new Vector3(1.1f), ease);
            scaleAnimation2.InsertKeyFrame(1.0f, new Vector3(1.0f), ease2);
            scaleAnimation2.DelayTime = TimeSpan.FromSeconds(0.15);
            scaleAnimation2.Duration = TimeSpan.FromSeconds(0.3);
            scaleAnimation2.Target = "Scale";

            clip.StartAnimationGroup(group);
            frameVisual.StartAnimation(scaleAnimation2.Target, scaleAnimation2);
        }

        splashVisual.StartAnimation("Opacity", splashHideAnimation);

        batch.End();
    }
}