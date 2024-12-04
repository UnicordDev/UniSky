﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using UniSky.Pages;
using UniSky.Services;
using UniSky.ViewModels.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace UniSky.Controls
{
    [TemplatePart(Name = "PART_TextBlock", Type = typeof(TextBlock))]
    public sealed class RichTextBlock : Control
    {
        private static readonly DependencyProperty HyperlinkUrlProperty =
            DependencyProperty.RegisterAttached("HyperlinkUrl", typeof(Uri), typeof(RichTextBlock), new PropertyMetadata(null));

        public TextWrapping TextWrapping
        {
            get { return (TextWrapping)GetValue(TextWrappingProperty); }
            set { SetValue(TextWrappingProperty, value); }
        }

        public static readonly DependencyProperty TextWrappingProperty =
            DependencyProperty.Register("TextWrapping", typeof(TextWrapping), typeof(RichTextBlock), new PropertyMetadata(TextWrapping.Wrap));

        public bool IsTextSelectionEnabled
        {
            get { return (bool)GetValue(IsTextSelectionEnabledProperty); }
            set { SetValue(IsTextSelectionEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsTextSelectionEnabledProperty =
            DependencyProperty.Register("IsTextSelectionEnabled", typeof(bool), typeof(RichTextBlock), new PropertyMetadata(true));

        public IList<FacetInline> Inlines
        {
            get { return (IList<FacetInline>)GetValue(InlinesProperty); }
            set { SetValue(InlinesProperty, value); }
        }

        public static readonly DependencyProperty InlinesProperty =
            DependencyProperty.Register("Inlines", typeof(IList<FacetInline>), typeof(RichTextBlock), new PropertyMetadata(null, OnInlinesChanged));

        private static void OnInlinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not RichTextBlock text)
                return;

            text.UpdateInlines();
        }

        public RichTextBlock()
        {
            this.DefaultStyleKey = typeof(RichTextBlock);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            UpdateInlines();
        }

        private void UpdateInlines()
        {
            this.ApplyTemplate();

            if (this.FindDescendantByName("PART_TextBlock") is not TextBlock text)
                return;

            text.Inlines.Clear();

            // TODO: this could be cleaner
            foreach (var inline in Inlines)
            {
                var mention = inline.Properties.OfType<MentionProperty>()
                                               .FirstOrDefault();
                if (mention != null)
                {
                    var hyperlink = new Hyperlink()
                    {
                        Inlines = { new Run() { Text = inline.Text } }
                    };

                    hyperlink.Click += Hyperlink_Click;
                    hyperlink.SetValue(HyperlinkUrlProperty, new Uri("unisky:///profile/" + mention.Did.ToString()));

                    text.Inlines.Add(hyperlink);
                    continue;
                }

                var tag = inline.Properties.OfType<TagProperty>()
                                           .FirstOrDefault();
                if (tag != null)
                {
                    var hyperlink = new Hyperlink()
                    {
                        Inlines = { new Run() { Text = inline.Text } }
                    };

                    hyperlink.Click += Hyperlink_Click;
                    hyperlink.SetValue(HyperlinkUrlProperty, new Uri("unisky:///tag/" + tag.Tag));

                    text.Inlines.Add(hyperlink);
                    continue;
                }

                var link = inline.Properties.OfType<LinkProperty>()
                                            .FirstOrDefault();
                if (link != null && Uri.TryCreate(link.Url, UriKind.Absolute, out var uri))
                {
                    var hyperlink = new Hyperlink()
                    {
                        NavigateUri = uri,
                        Inlines = { new Run() { Text = inline.Text } }
                    };

                    text.Inlines.Add(hyperlink);
                    continue;
                }

                text.Inlines.Add(new Run() { Text = inline.Text });
            }
        }

        // TODO: move this somewhere better
        private void Hyperlink_Click(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            if (sender.GetValue(HyperlinkUrlProperty) is not Uri { Scheme: "unisky" } uri)
                return;

            var service = Ioc.Default.GetRequiredService<INavigationServiceLocator>()
                .GetNavigationService("Home");

            var path = uri.PathAndQuery.Split('/', StringSplitOptions.RemoveEmptyEntries);
            switch (path.FirstOrDefault()?.ToLowerInvariant())
            {
                case "profile":
                    service.Navigate<ProfilePage>(uri);
                    break;
                case "tag":
                    break;
            }
        }
    }
}