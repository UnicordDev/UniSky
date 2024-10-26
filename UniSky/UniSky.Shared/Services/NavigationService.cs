using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml;

namespace UniSky.Services;

internal class NavigationService : INavigationService
{
    public event NavigatedEventHandler Navigated;
    public event NavigationFailedEventHandler NavigationFailed;

    private Frame _frame;
    private object _lastParamUsed;

    public Frame Frame
    {
        get
        {
            if (_frame == null)
            {
                _frame = Window.Current.Content as Frame;
                RegisterFrameEvents();
            }

            return _frame;
        }

        set
        {
            UnregisterFrameEvents();
            _frame = value;
            RegisterFrameEvents();
        }
    }

    public bool CanGoBack => Frame.CanGoBack;

    public bool CanGoForward => Frame.CanGoForward;

    public bool GoBack()
    {
        if (CanGoBack)
        {
            Frame.GoBack();
            return true;
        }

        return false;
    }

    public void GoForward() => Frame.GoForward();

    public bool Navigate(Type pageType, object parameter = null, NavigationTransitionInfo infoOverride = null)
    {
        if (pageType == null || !typeof(Page).IsAssignableFrom(pageType))
        {
            throw new ArgumentException($"Invalid pageType '{pageType}', please provide a valid pageType.", nameof(pageType));
        }

        // Don't open the same page multiple times
        if (Frame.Content?.GetType() != pageType || (parameter != null && !parameter.Equals(_lastParamUsed)))
        {
            var navigationResult = Frame.Navigate(pageType, parameter, infoOverride);
            if (navigationResult)
            {
                _lastParamUsed = parameter;
            }

            return navigationResult;
        }
        else
        {
            return false;
        }
    }

    public bool Navigate<T>(object parameter = null, NavigationTransitionInfo infoOverride = null)
        where T : Page
        => Navigate(typeof(T), parameter, infoOverride);

    private void RegisterFrameEvents()
    {
        if (_frame != null)
        {
            _frame.Navigated += Frame_Navigated;
            _frame.NavigationFailed += Frame_NavigationFailed;
        }
    }

    private void UnregisterFrameEvents()
    {
        if (_frame != null)
        {
            _frame.Navigated -= Frame_Navigated;
            _frame.NavigationFailed -= Frame_NavigationFailed;
        }
    }

    private void Frame_NavigationFailed(object sender, NavigationFailedEventArgs e) => NavigationFailed?.Invoke(sender, e);

    private void Frame_Navigated(object sender, NavigationEventArgs e) => Navigated?.Invoke(sender, e);
}
