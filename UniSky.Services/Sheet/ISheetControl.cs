using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml;

namespace UniSky.Services;

public interface ISheetControl : IOverlayControl
{
    bool IsPrimaryButtonEnabled { get; set; }
    ICommand PrimaryButtonCommand { get; set; }
    object PrimaryButtonContent { get; set; }
    DataTemplate PrimaryButtonContentTemplate { get; set; }
    Visibility PrimaryButtonVisibility { get; set; }
    bool IsSecondaryButtonEnabled { get; set; }
    ICommand SecondaryButtonCommand { get; set; }
    object SecondaryButtonContent { get; set; }
    DataTemplate SecondaryButtonContentTemplate { get; set; }
    Visibility SecondaryButtonVisibility { get; set; }
}
