using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace UniSky.Controls.Overlay;

public class OverlayHidingEventArgs : RoutedEventArgs
{
    private Deferral _deferral;
    private TaskCompletionSource<object> _deferralCompletion;

    public Deferral GetDeferral()
    {
        _deferralCompletion = new TaskCompletionSource<object>();
        return (_deferral ??= new Deferral(OnDeferralCompleted));
    }

    public bool Cancel { get; set; } = false;

    public Task WaitOnDeferral()
    {
        if (_deferral == null)
            return Task.CompletedTask;
        else
            return _deferralCompletion.Task;
    }

    private void OnDeferralCompleted()
    {
        _deferralCompletion?.SetResult(null);
    }
}
