using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using FishyFlip.Models;
using UniSky.Services.Navigation;
using UniSky.ViewModels.Error;

namespace UniSky.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    private class LoadingContext : IDisposable
    {
        private ViewModelBase viewModel;

        public LoadingContext(ViewModelBase viewModel)
        {
            this.viewModel = viewModel;
            this.viewModel.IsLoading = true;
        }

        public void Dispose()
        {
            if (this.viewModel != null)
                this.viewModel.IsLoading = false;
            this.viewModel = null;
        }
    }

    /// <summary>
    /// Indicates if the ViewModel is currently loading data. For easier use, see <see cref="GetLoadingContext"/>.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Holds the error state of the current ViewModel.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsErrored))]
    private ErrorViewModel _error;

    /// <summary>
    /// Indicates if the view model is in an error state.
    /// </summary>
    public bool IsErrored
        => Error != null;

    protected SynchronizationContext syncContext;

    private readonly INavigationContext _navigation;

    public ViewModelBase()
        : this(null)
    {
    }

    /// <param name="navigation">
    /// The scope this viewmodel navigates within, handed down by whoever created it.
    /// </param>
    public ViewModelBase(INavigationContext navigation)
        : base()
    {
        this._navigation = navigation;
        this.syncContext = SynchronizationContext.Current;
        Debug.Assert(syncContext != null, "Synchronisation context was null! Make sure you're creating this ViewModel on the UI thread.");
    }

    /// <summary>
    /// Where this viewmodel's navigation requests go.
    /// </summary>
    public INavigationContext Navigation
        => _navigation ?? NavigationScopes.ShellForCurrentView;

    /// <summary>
    /// Raises a navigation request against this viewmodel's scope.
    /// </summary>
    /// <returns><see langword="true"/> if something handled it.</returns>
    protected bool Navigate(NavigationRequest request)
        => request != null && (Navigation?.Navigate(request) ?? false);

    /// <summary>
    /// Retruns an object implementing <see cref="IDisposable"/> that sets <see cref="IsLoading"/> to 
    /// <see langword="true"/>, then upon <see cref="IDisposable.Dispose"/> reverts the value to <see langword="false"/>.
    /// </summary>
    public IDisposable GetLoadingContext()
        => new LoadingContext(this);

    partial void OnErrorChanged(ErrorViewModel value)
    {
        if (value != null)
            this.IsLoading = false;
    }

    partial void OnIsLoadingChanged(bool value)
    {
        this.OnLoadingChanged(value);
    }

    protected virtual void SetErrored(object e)
    {
        this.syncContext.Post(o =>
        {
            if (o is Exception ex)
                this.Error = new ExceptionViewModel(ex);
            else if (o is ATError e)
                this.Error = new ATErrorViewModel(e);
            else
                this.Error = null;
        }, e);
    }

    protected void OnPropertyChanged(params string[] names)
    {
        foreach (var name in names)
        {
            base.OnPropertyChanged(name);
        }
    }

    protected virtual void OnLoadingChanged(bool value)
    {

    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        this.syncContext.Post((o) => base.OnPropertyChanged((PropertyChangedEventArgs)o), e);
    }

    protected override void OnPropertyChanging(PropertyChangingEventArgs e)
    {
        this.syncContext.Post((o) => base.OnPropertyChanging((PropertyChangingEventArgs)o), e);
    }
}
