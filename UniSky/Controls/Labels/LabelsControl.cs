using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace UniSky.Controls.Labels;

public sealed class LabelsControl : Control
{
    public object Labels
    {
        get { return (object)GetValue(LabelsProperty); }
        set { SetValue(LabelsProperty, value); }
    }

    public static readonly DependencyProperty LabelsProperty =
        DependencyProperty.Register("Labels", typeof(object), typeof(LabelsControl), new PropertyMetadata(DependencyProperty.UnsetValue));

    public LabelsControl()
    {
        this.DefaultStyleKey = typeof(LabelsControl);
    }
}
