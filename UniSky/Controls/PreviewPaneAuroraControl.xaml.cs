using Microsoft.Toolkit.Uwp.Helpers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;
using ColorHelper = Microsoft.Toolkit.Uwp.Helpers.ColorHelper;

namespace System.Windows.Shell.Aurora
{
    /// <summary>
    /// Interaction logic for PreviewPaneAuroraControl.xaml
    /// </summary>
    public partial class PreviewPaneAuroraControl : UserControl
    {
        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(Color), typeof(PreviewPaneAuroraControl), new PropertyMetadata(Color.FromArgb(255, 0x85, 0x99, 0xB4), OnColorChanged));

        public TimeSpan AnimationDuration
        {
            get => (TimeSpan)GetValue(AnimationDurationProperty);
            set => SetValue(AnimationDurationProperty, value);
        }

        // Using a DependencyProperty as the backing store for AnimationDuration.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AnimationDurationProperty =
            DependencyProperty.Register("AnimationDuration", typeof(TimeSpan), typeof(PreviewPaneAuroraControl), new PropertyMetadata(TimeSpan.FromSeconds(0.5)));

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var oldValue = (Color)e.OldValue;
            var newValue = (Color)e.NewValue;
            var control = (PreviewPaneAuroraControl)d;

            var hslColor = newValue.ToHsl();
            if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
            {
                var hslBackground = hslColor with { L = hslColor.L * 0.7 };
                var colorBackground = ColorHelper.FromHsl(hslBackground.H, hslBackground.S, hslBackground.L);

                control._AnimateAurora(oldValue, newValue, colorBackground);
            }
            else
            {
                var hslForeground = hslColor with { L = Math.Clamp(hslColor.L * 1.25, 0, 1) };
                var colorForeground = ColorHelper.FromHsl(hslForeground.H, hslForeground.S, hslForeground.L);

                control._AnimateAurora(oldValue, colorForeground, newValue);
            }
        }

        public PreviewPaneAuroraControl()
        {
            InitializeComponent();
        }

        private void _AnimateAurora(Color colorOld, Color colorAurora, Color colorBackground)
        {
            var storyboard = new Storyboard();
            if (BackgroundLayer.Background is not SolidColorBrush backgroundBrush)
                backgroundBrush = new SolidColorBrush();

            // RegisterName("BackgroundLayerBrush", backgroundBrush);

            var backgroundAnim = new ColorAnimation();
            backgroundAnim.From = backgroundBrush.Color;
            backgroundAnim.To = colorBackground;
            backgroundAnim.Duration = new Duration(AnimationDuration);

            Storyboard.SetTarget(backgroundAnim, backgroundBrush);
            Storyboard.SetTargetProperty(backgroundAnim, "Color");
            storyboard.Children.Add(backgroundAnim);

            this._AdjustAurora(storyboard, colorOld, colorAurora, BackgroundLayer);

            storyboard.Begin();
        }

        private void _AdjustAurora(Storyboard sb, Color colorOld, Color colorNew, UIElement pe)
        {
            if (pe is Shape shape)
            {
                if (shape.Fill is GradientBrush fill)
                {
                    Color colorNew1 = colorNew;
                    Color colorOld1 = colorOld;
                    this._AdjustedLinearGradient(sb, fill, colorOld1, colorNew1);
                }
                if (shape.Stroke is GradientBrush stroke)
                {
                    Color colorNew2 = colorNew;
                    Color colorOld2 = colorOld;
                    this._AdjustedLinearGradient(sb, stroke, colorOld2, colorNew2);
                }
            }


            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(pe); i++)
            {
                var child = VisualTreeHelper.GetChild(pe, i);
                if (child is not UIElement element) continue;

                Color colorNew3 = colorNew;
                this._AdjustAurora(sb, colorOld, colorNew3, element);
            }
        }

        private void _AdjustedLinearGradient(
            Storyboard sb,
            GradientBrush pLinearGradient,
            Color colorOld,
            Color colorNew)
        {
            GradientStopCollection gradientStops = pLinearGradient.GradientStops;
            for (int i = 0; i < gradientStops.Count; i++)
            {
                GradientStop gradientStop = gradientStops[i];
                Color color1 = gradientStop.Color;
                Color color2 = gradientStop.Color;
                Color color1_1 = Color.FromArgb(255, gradientStop.Color.R, color2.G, color1.B);
                Color color2_1 = colorOld;
                Color color4 = !AreClose(color1_1, color2_1) ?
                    gradientStop.Color :
                    Color.FromArgb(gradientStop.Color.A, colorNew.R, colorNew.G, colorNew.B);

                var colorAnimation = new ColorAnimation();
                colorAnimation.From = color1;
                colorAnimation.To = color4;
                colorAnimation.Duration = new Duration(AnimationDuration);
                colorAnimation.EnableDependentAnimation = true;

                Storyboard.SetTarget(colorAnimation, gradientStop);
                Storyboard.SetTargetProperty(colorAnimation, "Color");

                sb.Children.Add(colorAnimation);
            }
        }

        private bool AreClose(Color color1, Color color2)
        {
            return AreClose(color1.R, color2.R) && AreClose(color1.G, color2.G) && AreClose(color1.B, color2.B) && AreClose(color1.A, color2.A);
        }

        internal static float FLT_EPSILON = 1.1920929E-07f;

        public static bool AreClose(float a, float b)
        {
            if (a == b)
            {
                return true;
            }

            float num = (Math.Abs(a) + Math.Abs(b) + 10f) * FLT_EPSILON;
            float num2 = a - b;
            if (0f - num < num2)
            {
                return num > num2;
            }

            return false;
        }

    }
}
