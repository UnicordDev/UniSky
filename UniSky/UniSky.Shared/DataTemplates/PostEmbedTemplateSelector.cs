using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniSky.ViewModels.Posts;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniSky.DataTemplates
{
    internal class PostEmbedTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ImagesEmbedTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return item switch
            {
                PostEmbedImagesViewModel => ImagesEmbedTemplate,
                _ => null,
            };
        }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            return SelectTemplateCore(item, null);
        }
    }
}
