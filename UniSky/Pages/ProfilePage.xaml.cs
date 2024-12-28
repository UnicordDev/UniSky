using System;
using System.Numerics;
using FishyFlip.Lexicon;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Toolkit.Uwp.UI.Animations.Expressions;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using UniSky.Services;
using UniSky.ViewModels.Profile;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using EF = Microsoft.Toolkit.Uwp.UI.Animations.Expressions.ExpressionFunctions;

namespace UniSky.Pages;

public sealed partial class ProfilePage : Page, IScrollToTop
{
    private const string PROGRESS_NODE = "progress";
    private const string PIXELS_TO_MOVE_NODE = "pixelsToMove";
    private const string SCALE_FACTOR_NODE = "scaleFactor";
    private const string TOTAL_SIZE_NODE = "totalSize";
    private const string HEADER_SIZE_NODE = "headerSize";
    private const string FOOTER_SIZE_NODE = "footerSize";
    private const string STICKY_SIZE_NODE = "stickySize";
    private const string TEXT_SIZE_NODE = "textSize";
    private const string IMAGE_SIZE_NODE = "imageSize";

    private Compositor _compositor;
    private CompositionPropertySet _props;
    private CompositionPropertySet _scrollerPropertySet;
    private Visual _headerGrid;
    private Visual _profileImage;
    private Visual _profileContainer;
    private Visual _textContainer;
    private Visual _subTextContainer;
    private Visual _scrolledDisplayNameContainer;
    private SpriteVisual _blurredBackgroundImageVisual;

    public ProfilePageViewModel ViewModel
    {
        get => (ProfilePageViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register("ViewModel", typeof(ProfilePageViewModel), typeof(ProfilePage), new PropertyMetadata(null));

    public ProfilePage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var safeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();
        safeAreaService.SetTitlebarTheme(ElementTheme.Default);
        safeAreaService.SafeAreaUpdated += OnSafeAreaUpdated;

        if (e.Parameter is Uri { Scheme: "unisky" } uri)
            HandleUniskyProtocol(uri);
        else if (e.Parameter is ATObject basic)
            this.DataContext = ViewModel = ActivatorUtilities.CreateInstance<ProfilePageViewModel>(ServiceContainer.Default, basic);
        else
            this.DataContext = ViewModel = ActivatorUtilities.CreateInstance<ProfilePageViewModel>(ServiceContainer.Default);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        var safeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();
        safeAreaService.SafeAreaUpdated -= OnSafeAreaUpdated;
        safeAreaService.SetTitlebarTheme(ElementTheme.Default);
    }

    private void HandleUniskyProtocol(Uri uri)
    {
        var path = uri.PathAndQuery.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (path.Length < 2 || !string.Equals(path[0], "profile", StringComparison.InvariantCultureIgnoreCase))
        {
            this.Frame.Navigate(typeof(FeedsPage));
        }

        if (ATDid.TryCreate(path[1], out var did))
            this.DataContext = ViewModel = ActivatorUtilities.CreateInstance<ProfilePageViewModel>(ServiceContainer.Default, did);
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // Retrieve the ScrollViewer that the GridView is using internally
        var scrollViewer = RootList.FindDescendant<ScrollViewer>();

        // Update the ZIndex of the header container so that the header is above the items when scrolling
        var headerPresenter = (UIElement)VisualTreeHelper.GetParent((UIElement)RootList.Header);
        var headerContainer = (UIElement)VisualTreeHelper.GetParent(headerPresenter);
        Canvas.SetZIndex(headerContainer, 1);

        // Get the PropertySet that contains the scroll values from the ScrollViewer
        _scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
        _compositor = _scrollerPropertySet.Compositor;

        // snag all the element visuals
        _headerGrid = ElementCompositionPreview.GetElementVisual(HeaderGrid);
        _profileImage = ElementCompositionPreview.GetElementVisual(ProfileImage);
        _profileContainer = ElementCompositionPreview.GetElementVisual(ProfileContainer);
        _textContainer = ElementCompositionPreview.GetElementVisual(TextContainer);
        _subTextContainer = ElementCompositionPreview.GetElementVisual(SubTextContainer);
        _scrolledDisplayNameContainer = ElementCompositionPreview.GetElementVisual(ScrolledDisplayNameContainer);

        // offset doesn't always work so enable translation on a few of these
        ElementCompositionPreview.SetIsTranslationEnabled(ProfileImage, true);
        ElementCompositionPreview.SetIsTranslationEnabled(ScrolledDisplayNameContainer, true);

        // create the background visual that will host the blur for the header image
        _blurredBackgroundImageVisual = _compositor.CreateSpriteVisual();

        // properties, most of these get updated when the size changes
        _props = _compositor.CreatePropertySet();
        _props.InsertScalar(PROGRESS_NODE, 0);

        UpdateSizeDependentProperties();

        // grab the properties of the scroll view
        var scrollingProperties = _scrollerPropertySet.GetSpecializedReference<ManipulationPropertySetReferenceNode>();

        var props = _props.GetReference();
        var progressNode = props.GetScalarProperty(PROGRESS_NODE);
        var pixelsToMoveNode = props.GetScalarProperty(PIXELS_TO_MOVE_NODE);
        var scaleFactorNode = props.GetScalarProperty(SCALE_FACTOR_NODE);

        var totalSizeNode = props.GetScalarProperty(TOTAL_SIZE_NODE);

        var headerSizeNode = props.GetScalarProperty(HEADER_SIZE_NODE);
        var footerSizeNode = props.GetScalarProperty(FOOTER_SIZE_NODE);
        var stickySizeNode = props.GetScalarProperty(STICKY_SIZE_NODE);
        var textSizeNode = props.GetScalarProperty(TEXT_SIZE_NODE);
        var imageSizeNode = props.GetScalarProperty(IMAGE_SIZE_NODE);

        var blurEffect = new GaussianBlurEffect()
        {
            Name = "blur",
            BlurAmount = 0.0f,
            BorderMode = EffectBorderMode.Hard,
            Optimization = EffectOptimization.Balanced,
            Source = new CompositionEffectSourceParameter("source")
        };

        var effect = new ExposureEffect()
        {
            Name = "tint",
            Source = new CompositionEffectSourceParameter("source"),
        };

        var blurBrush = _compositor.CreateEffectFactory(blurEffect, ["blur.BlurAmount"])
            .CreateBrush();

        //var tintBrush = _compositor.CreateEffectFactory(effect, ["tint.Exposure"])
        //    .CreateBrush();

        blurBrush.SetSourceParameter("source", _compositor.CreateBackdropBrush());
        //tintBrush.SetSourceParameter("source", blurBrush);

        _blurredBackgroundImageVisual.Brush = blurBrush;
        ElementCompositionPreview.SetElementChildVisual(ProfileBanner, _blurredBackgroundImageVisual);

        // animate the progress value
        var progressAnimation = EF.Clamp(-scrollingProperties.Translation.Y / pixelsToMoveNode, 0, 1);
        _props.StartAnimation(PROGRESS_NODE, progressAnimation);

        // animate the blurriness with respect to progress
        var blurAnimation = EF.Lerp(0, 20, progressNode);
        blurBrush.Properties.StartAnimation("blur.BlurAmount", blurAnimation);

        //var tintAnimation = EF.Lerp(0, -0.25f, progressNode);
        //tintBrush.Properties.StartAnimation("tint.Exposure", tintAnimation);

        // move everything with the scroll viewer, make it sticky
        var headerTranslationAnimation = EF.Conditional(progressNode < 1, 0, -scrollingProperties.Translation.Y - pixelsToMoveNode);
        _profileContainer.StartAnimation("Offset.Y", headerTranslationAnimation);

        // move the header image with relation to the scroll amount, making sure to not overlap the sticky section
        var headerGridTranslationAnimation = (EF.Min(pixelsToMoveNode, totalSizeNode - headerSizeNode - stickySizeNode)) * progressNode;
        _headerGrid.StartAnimation("Offset.Y", headerGridTranslationAnimation);

        // create a springy effect when overscrolled
        var headerScaleAnimation = EF.Lerp(1, 1.25f, EF.Clamp(scrollingProperties.Translation.Y / 50, 0, 1));
        _headerGrid.StartAnimation("Scale.X", headerScaleAnimation);
        _headerGrid.StartAnimation("Scale.Y", headerScaleAnimation);

        // move the profile image to the bottom of the sticky header
        var imageOffset = footerSizeNode + EF.Max(0, textSizeNode - imageSizeNode - 16); // 16 == margin.top + margin.bottom
        var translateAnimation = EF.Vector3(0, EF.Lerp(0, imageOffset, progressNode), 0);
        _profileImage.StartAnimation("Translation", translateAnimation);

        // scale the profile image down
        var scaleAnimation = EF.Lerp(1, scaleFactorNode, progressNode);
        _profileImage.StartAnimation("Scale.X", scaleAnimation);
        _profileImage.StartAnimation("Scale.Y", scaleAnimation);

        // fade out the original text
        var textOpacityAnimation = EF.Clamp(1 - (progressNode * 1.5f), 0, 1);
        _textContainer.StartAnimation("Opacity", textOpacityAnimation);

        // fade in and scroll up newly aligned text to replace the old ones
        var scrolledTextOpacityAnimation = EF.Clamp((progressNode - 0.5f) * 2f, 0, 1);
        var scrolledTextTranslateAnimation = EF.Vector3(0, EF.Lerp(0, footerSizeNode, progressNode), 0);
        _scrolledDisplayNameContainer.StartAnimation("Translation", scrolledTextTranslateAnimation);
        _scrolledDisplayNameContainer.StartAnimation("Opacity", scrolledTextOpacityAnimation);
    }

    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateSizeDependentProperties();
    }

    private void OnSafeAreaUpdated(object sender, SafeAreaUpdatedEventArgs e)
    {
        UpdateSizeDependentProperties();
    }

    private void UpdateSizeDependentProperties()
    {
        if (ProfileContainer.ActualHeight == 0)
            return;

        var safeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();

        var titleBarHeight = (float)safeAreaService.State.Bounds.Top + 4;
        var stickyHeight = (float)StickyFooter.ActualHeight;
        var totalSize = (float)ProfileContainer.ActualHeight;
        var clampHeight = (float)(52 + titleBarHeight) + stickyHeight;
        var pixelsToMove = (float)(totalSize - clampHeight);

        if (_props != null)
        {
            _props.InsertScalar(TOTAL_SIZE_NODE, totalSize);
            _props.InsertScalar(PIXELS_TO_MOVE_NODE, pixelsToMove);
            _props.InsertScalar(STICKY_SIZE_NODE, stickyHeight);
            _props.InsertScalar(HEADER_SIZE_NODE, (float)HeaderGrid.ActualHeight);
            _props.InsertScalar(FOOTER_SIZE_NODE, (float)SubTextContainer.ActualHeight);
            _props.InsertScalar(IMAGE_SIZE_NODE, (float)ProfileImage.ActualHeight);
            _props.InsertScalar(TEXT_SIZE_NODE, (float)TextContainer.ActualHeight);
            _props.InsertScalar(SCALE_FACTOR_NODE, (float)(44.0 / ProfileImage.ActualHeight));
        }

        if (_blurredBackgroundImageVisual != null)
            _blurredBackgroundImageVisual.Size = new Vector2((float)ProfileBanner.ActualWidth, (float)ProfileBanner.ActualHeight);
        if (_headerGrid != null)
            _headerGrid.CenterPoint = new Vector3((float)(HeaderGrid.ActualWidth / 2), (float)HeaderGrid.ActualHeight, 0);
        if (_textContainer != null)
            _textContainer.CenterPoint = new Vector3((float)TextContainer.ActualWidth / 2, (float)TextContainer.ActualHeight / 2, 0);
        if (_profileImage != null)
            _profileImage.CenterPoint = new Vector3(4, (float)ProfileImage.ActualHeight + 4, 0);
    }

    private void ProfileContainer_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (e.Handled) return;

        ScrollToTop();
    }

    public void ScrollToTop()
    {
        var scrollView = RootList.FindDescendant<ScrollViewer>();
        if (scrollView is null)
            return;

        scrollView.ChangeView(0, 0, null);
    }
}
