using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.Graphics.Display;
using System.Threading;

namespace UniSky.Extensions
{
    public class Hairline
    {
        public static Thickness GetBorderThickness(DependencyObject obj)
        {
            return (Thickness)obj.GetValue(BorderThicknessProperty);
        }

        public static void SetBorderThickness(DependencyObject obj, Thickness value)
        {
            obj.SetValue(BorderThicknessProperty, value);
        }

        public static readonly DependencyProperty BorderThicknessProperty =
            DependencyProperty.RegisterAttached("BorderThickness", typeof(Thickness), typeof(Hairline), new PropertyMetadata(new Thickness(-1), OnThicknessPropertyChanged));

        public static Thickness GetMargin(DependencyObject obj)
        {
            return (Thickness)obj.GetValue(MarginProperty);
        }

        public static void SetMargin(DependencyObject obj, Thickness value)
        {
            obj.SetValue(MarginProperty, value);
        }

        public static readonly DependencyProperty MarginProperty =
            DependencyProperty.RegisterAttached("Margin", typeof(Thickness), typeof(Hairline), new PropertyMetadata(new Thickness(-1), OnMarginPropertyChanged));

        private static ThreadLocal<List<WeakReference<DependencyObject>>> Elements { get; }
            = new ThreadLocal<List<WeakReference<DependencyObject>>>(() => new List<WeakReference<DependencyObject>>(), true);

        private static ThreadLocal<DisplayInformation> DisplayInfo { get; }
            = new ThreadLocal<DisplayInformation>(() =>
            {
                var info = DisplayInformation.GetForCurrentView();
                info.DpiChanged += OnDpiChanged;
                return info;
            }, true);

        private static void OnThicknessPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not (Grid or StackPanel or Control or Border))
                return;

            Elements.Value.Add(new WeakReference<DependencyObject>(d));

            if (DisplayInfo == null)
            {
                Initialize();
            }

            var newValue = (Thickness)(e.NewValue);
            ApplyBorderThickness(d, newValue, DisplayInfo.Value);
        }

        private static void OnMarginPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement)
                return;

            Elements.Value.Add(new WeakReference<DependencyObject>(d));

            if (DisplayInfo == null)
            {
                Initialize();
            }

            var newValue = (Thickness)(e.NewValue);
            ApplyMargin(d, newValue, DisplayInfo.Value);
        }

        private static void ApplyBorderThickness(DependencyObject d, Thickness newValue, DisplayInformation info)
        {
            if (newValue.Left < 0 || newValue.Top < 0 || newValue.Right < 0 || newValue.Bottom < 0)
                return;

            var hairlineThickness = (1.0 / (info.LogicalDpi / 96.0));
            var thickness = new Thickness(
                newValue.Left * hairlineThickness,
                newValue.Top * hairlineThickness,
                newValue.Right * hairlineThickness,
                newValue.Bottom * hairlineThickness);

            switch (d)
            {
                case Grid g:
                    g.BorderThickness = thickness;
                    break;
                case Border p:
                    p.BorderThickness = thickness;
                    break;
                case Control c:
                    c.BorderThickness = thickness;
                    break;
                case StackPanel s:
                    s.BorderThickness = thickness;
                    break;
            }
        }

        private static void ApplyMargin(DependencyObject d, Thickness newValue, DisplayInformation info)
        {
            if (newValue.Left < 0 || newValue.Top < 0 || newValue.Right < 0 || newValue.Bottom < 0)
                return;

            var hairlineThickness = (1.0 / (info.LogicalDpi / 96.0));
            var thickness = new Thickness(
                newValue.Left * hairlineThickness,
                newValue.Top * hairlineThickness,
                newValue.Right * hairlineThickness,
                newValue.Bottom * hairlineThickness);

            if (d is FrameworkElement element)
                element.Margin = thickness;
        }

        public static void Initialize()
        {
            ApplyResources(DisplayInfo.Value);
        }

        private static void OnDpiChanged(DisplayInformation sender, object args)
        {
            foreach (var item in Elements.Value)
            {
                if (item.TryGetTarget(out var element))
                {
                    var thicknessVal = element.GetValue(BorderThicknessProperty);
                    if (thicknessVal != DependencyProperty.UnsetValue && thicknessVal is Thickness thickness)
                        ApplyBorderThickness(element, thickness, sender);

                    thicknessVal = element.GetValue(MarginProperty);
                    if (thicknessVal != DependencyProperty.UnsetValue && thicknessVal is Thickness margin)
                        ApplyMargin(element, margin, sender);
                }
            }

            ApplyResources(sender);
        }

        private static void ApplyResources(DisplayInformation info)
        {
            var hairlineWidth = (1.0 / (info.LogicalDpi / 96.0));
            var hairlineThickness = new Thickness(hairlineWidth);

            var rootDictionary = new ResourceDictionary();
            rootDictionary["MenuBarItemBorderThickness"] = hairlineThickness;
            rootDictionary["GridViewItemMultiselectBorderThickness"] = hairlineThickness;
            rootDictionary["CheckBoxBorderThemeThickness"] = hairlineThickness;
            rootDictionary["GridViewItemSelectedBorderThemeThickness"] = hairlineWidth;
            rootDictionary["RadioButtonBorderThemeThickness"] = hairlineWidth;
            rootDictionary["ButtonBorderThemeThickness"] = hairlineThickness;
            rootDictionary["CalendarDatePickerBorderThemeThickness"] = hairlineThickness;
            rootDictionary["TimePickerBorderThemeThickness"] = hairlineThickness;
            rootDictionary["DatePickerBorderThemeThickness"] = hairlineThickness;
            rootDictionary["ToggleSwitchOuterBorderStrokeThickness"] = hairlineThickness;
            rootDictionary["RepeatButtonBorderThemeThickness"] = hairlineThickness;
            rootDictionary["SearchBoxBorderThemeThickness"] = hairlineThickness;
            rootDictionary["ToggleButtonBorderThemeThickness"] = hairlineThickness;
            rootDictionary["TextControlBorderThemeThickness"] = hairlineThickness;
            rootDictionary["ButtonRevealBorderThemeThickness"] = hairlineThickness;
            rootDictionary["RepeatButtonRevealBorderThemeThickness"] = hairlineThickness;
            rootDictionary["ToggleButtonRevealBorderThemeThickness"] = hairlineThickness;
            rootDictionary["AppBarEllipsisButtonRevealBorderThemeThickness"] = hairlineThickness;
            rootDictionary["AppBarButtonRevealBorderThemeThickness"] = hairlineThickness;
            rootDictionary["AppBarToggleButtonRevealBorderThemeThickness"] = hairlineThickness;
            rootDictionary["ListViewItemRevealBorderThemeThickness"] = hairlineThickness;
            rootDictionary["GridViewItemRevealBorderThemeThickness"] = hairlineThickness;
            rootDictionary["ComboBoxItemRevealBorderThemeThickness"] = hairlineThickness;
            rootDictionary["PersonPictureEllipseBadgeStrokeThickness"] = hairlineWidth;

            App.Current.Resources.MergedDictionaries[0].MergedDictionaries.Insert(0, rootDictionary);

            if (Window.Current.Content is FrameworkElement element)
            {
                var currentTheme = element.ActualTheme;
                var invert = currentTheme switch
                {
                    ElementTheme.Dark => ElementTheme.Light,
                    ElementTheme.Light => ElementTheme.Dark,
                    _ => ElementTheme.Light
                };
                element.RequestedTheme = invert;
                element.RequestedTheme = ElementTheme.Default;
            }
        }
    }
}
