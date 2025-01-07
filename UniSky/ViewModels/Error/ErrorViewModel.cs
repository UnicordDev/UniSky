using CommunityToolkit.Mvvm.ComponentModel;

namespace UniSky.ViewModels.Error;

public abstract partial class ErrorViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _message;
}
