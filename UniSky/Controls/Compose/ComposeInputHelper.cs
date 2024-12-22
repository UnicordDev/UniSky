using System;
using System.Diagnostics;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UniSky.Controls.Compose;

public sealed class ComposeInputHelper
{
    private enum ComposeSelectionState
    {
        Standard,
        Programatic
    }

    private readonly FrameworkElement Container;
    private readonly TextBox PrimaryTextBox;
    private readonly Popup TextBoxPopup;
    private readonly TextBlock TextBoxPopupContent;

    private ScrollContentPresenter TextBoxScrollPresenter;
    private GeneralTransform TextBoxScrollTransform;

    private ComposeSelectionState SelectionState;
    private bool IsSelectionListOpen
    {
        get => TextBoxPopup.IsOpen;
        set => TextBoxPopup.IsOpen = value;
    }

    public ComposeInputHelper(FrameworkElement container, TextBox primaryTextBox, Popup textBoxPopup)
    {
        this.Container = container;
        this.PrimaryTextBox = primaryTextBox;
        this.TextBoxPopup = textBoxPopup;
        this.TextBoxPopupContent = new TextBlock() { HorizontalAlignment = HorizontalAlignment.Left, Width = 300, TextWrapping = TextWrapping.Wrap };
        this.TextBoxPopup.Child = new StackPanel() { Children = { TextBoxPopupContent }, Background = new SolidColorBrush(Colors.Red) };

        this.SelectionState = ComposeSelectionState.Standard;

        this.PrimaryTextBox.SelectionChanged += OnSelectionChanged;
    }

    // a lot of this is based on how AutoSuggestBox works
    public void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var isGamepad = e.OriginalKey >= VirtualKey.GamepadA && e.OriginalKey <= VirtualKey.GamepadRightThumbstickLeft;
        var wasHandled = false;

        switch (e.Key)
        {
            case VirtualKey.Left:
            case VirtualKey.Right:
                if (IsSelectionListOpen && isGamepad)
                {
                    wasHandled = true;
                }
                break;
            case VirtualKey.Up:
            case VirtualKey.Down:
                if (IsSelectionListOpen)
                {
                    // TODO: move selection
                }
                break;
            case VirtualKey.Tab:
                if (IsSelectionListOpen)
                {
                    IsSelectionListOpen = false;
                    PrimaryTextBox.Focus(FocusState.Keyboard);
                }
                break;
            case VirtualKey.Escape:
                IsSelectionListOpen = false;
                PrimaryTextBox.Focus(FocusState.Keyboard);
                wasHandled = true;
                break;
            case VirtualKey.Enter:
                // TODO: do selection
                IsSelectionListOpen = false;
                wasHandled = true;
                break;
        }

        if (wasHandled)
            e.Handled = true;
    }

    private void OnSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (PrimaryTextBox.Text.Length == 0)
        {
            IsSelectionListOpen = false;
            return;
        }

        if ((PrimaryTextBox.SelectionLength != 0))
        {
            if (SelectionState == ComposeSelectionState.Programatic)
            {
                SelectionState = ComposeSelectionState.Standard;
                return;
            }
            else
            {
                IsSelectionListOpen = false;
                return;
            }
        }

        if (!FindTag(out var tagText, out var startIndex, out var length))
        {
            IsSelectionListOpen = false;
            return;
        }

        var rect = GetRelativeRectFromCharacterIndex(PrimaryTextBox.SelectionStart, false);
        IsSelectionListOpen = true;
        TextBoxPopupContent.Text = tagText;

        var popupWidth = 300;
        var fontHeight = PrimaryTextBox.FontSize / 72.0 * 96.0;
        var caretPosition = new Point(rect.Left - (popupWidth / 2), rect.Top + fontHeight);
        var caretMinX = 0;
        var caretMaxX = (Container.ActualWidth - popupWidth);

        caretPosition = caretPosition with { X = Math.Max(caretMinX, Math.Min(caretPosition.X, caretMaxX)) };

        TextBoxPopup.HorizontalOffset = caretPosition.X;
        TextBoxPopup.VerticalOffset = caretPosition.Y;
    }

    private bool FindTag(out string tagText, out int startIndex, out int length)
    {
        tagText = null;
        startIndex = 0;
        length = 0;

        var currentText = PrimaryTextBox.Text;
        var currentIndex = PrimaryTextBox.SelectionStart - 1;

        var endIndex = currentIndex;
        var hasTag = false;

        // backtrack until we find a space (bad) or an @/# (good)
        while (currentIndex >= 0)
        {
            var character = currentText[currentIndex];
            if (character == '#' || character == '@')
            {
                hasTag = true;
                break;
            }

            if (char.IsWhiteSpace(character) || !(char.IsLetterOrDigit(character) || character == '.' || character == '-'))
                return false;

            currentIndex--;
        }

        if (!hasTag)
            return false;

        startIndex = currentIndex;
        length = (endIndex - currentIndex) + 1;
        tagText = currentText.Substring(startIndex, length);
        return hasTag;
    }

    public Rect GetRelativeRectFromCharacterIndex(int charIndex, bool trailingEdge)
    {
        if (charIndex == PrimaryTextBox.Text.Length)
        {
            charIndex -= 1;
            trailingEdge = true;
        }

        // Hack: UWP does not properly return the location compared to the control, so we need to calculate it.
        // https://stackoverflow.com/questions/50304918/under-uwp-getrectfromcharacterindex-does-not-return-values-adjusted-to-the-styl

        TextBoxScrollPresenter ??= PrimaryTextBox.FindDescendant<ScrollContentPresenter>();
        TextBoxScrollTransform ??= TextBoxScrollPresenter.TransformToVisual(Container);

        var caret = PrimaryTextBox.GetRectFromCharacterIndex(charIndex, trailingEdge);
        TextBoxScrollTransform.TryTransform(new Point(caret.Left, caret.Top), out var topLeft);
        TextBoxScrollTransform.TryTransform(new Point(caret.Right, caret.Bottom), out var bottomRight);

        return new Rect(topLeft, bottomRight);
    }

}
