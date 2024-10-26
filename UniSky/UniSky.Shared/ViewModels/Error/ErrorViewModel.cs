using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using UniSky.Helpers;

namespace UniSky.ViewModels.Error;

public abstract partial class ErrorViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _message;
}
