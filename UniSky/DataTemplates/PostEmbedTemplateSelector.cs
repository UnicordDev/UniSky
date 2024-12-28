using Microsoft.Extensions.DependencyInjection;
using UniSky.Services;
using UniSky.ViewModels.Posts;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniSky.DataTemplates;

internal class PostEmbedTemplateSelector : DataTemplateSelector
{
    private readonly ITypedSettings typedSettings
        = ServiceContainer.Scoped.GetRequiredService<ITypedSettings>();

    public DataTemplate VideoEmbedTemplate { get; set; }
    public DataTemplate InlineVideoEmbedTemplate { get; set; }
    public DataTemplate ImagesEmbedTemplate { get; set; }
    public DataTemplate PostEmbedTemplate { get; set; }
    public DataTemplate ExternalEmbedTemplate { get; set; }
    public DataTemplate RecordWithMediaEmbedTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            PostEmbedImagesViewModel => ImagesEmbedTemplate,
            PostEmbedVideoViewModel => typedSettings.VideosInFeeds ? InlineVideoEmbedTemplate : VideoEmbedTemplate,
            PostEmbedPostViewModel => PostEmbedTemplate,
            PostEmbedExternalViewModel => ExternalEmbedTemplate,
            PostEmbedRecordWithMediaViewModel => RecordWithMediaEmbedTemplate,
            _ => null,
        };
    }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return SelectTemplateCore(item, null);
    }
}
