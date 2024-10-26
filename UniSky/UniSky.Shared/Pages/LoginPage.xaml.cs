using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using UniSky.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UniSky.Pages;

public sealed partial class LoginPage : Page
{
    public LoginViewModel ViewModel
    {
        get { return (LoginViewModel)GetValue(ViewModelProperty); }
        set { SetValue(ViewModelProperty, value); }
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register("ViewModel", typeof(LoginViewModel), typeof(LoginPage), new PropertyMetadata(null));

    public LoginPage()
    {
        this.InitializeComponent();
        this.ViewModel = ActivatorUtilities.CreateInstance<LoginViewModel>(Ioc.Default);
    }

    public bool IsNotNull(object o) 
        => o is not null;

    public bool Is(bool b)
        => b;
}
