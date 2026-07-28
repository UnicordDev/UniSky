using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniSky.Services.Navigation;

namespace UniSky.ViewModels.Notifications;

public partial class NotificationsPageViewModel : ViewModelBase
{
    private int loaded;

    [ObservableProperty]
    private bool isEmpty;

    public NotificationsCollection Notifications { get; }

    public NotificationsPageViewModel(INavigationContext navigation)
        : base(navigation)
    {
        this.Notifications = new NotificationsCollection(this);
    }

    public Task EnsureLoadedAsync()
        => Interlocked.Exchange(ref loaded, 1) == 0 ? RefreshAsync() : Task.CompletedTask;

    [RelayCommand]
    public async Task RefreshAsync()
    {
        await Notifications.RefreshAsync();
    }

    internal void OnLoadError(Exception ex)
        => SetErrored(ex);

    internal void ClearError()
        => syncContext.Post(_ => Error = null, null);

    internal void UpdateIsEmpty(bool value)
        => syncContext.Post(o => IsEmpty = (bool)o, value);
}
