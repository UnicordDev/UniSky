using CommunityToolkit.Mvvm.ComponentModel;
using FishyFlip.Models;
using UniSky.Helpers;

namespace UniSky.ViewModels.Error;

public sealed partial class ATErrorViewModel : ErrorViewModel
{
    public ATErrorViewModel(ATError failure)
    {
        Message = failure.Detail?.Message ?? failure.StatusCode.ToString();
    }
}
