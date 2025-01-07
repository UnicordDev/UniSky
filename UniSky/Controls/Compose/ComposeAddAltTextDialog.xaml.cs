using UniSky.ViewModels.Compose;
using Windows.UI.Xaml.Controls;

namespace UniSky.Controls.Compose;

public sealed partial class ComposeAddAltTextDialog : ContentDialog
{
    public ComposeAddAltTextDialog(ComposeViewAttachmentViewModel viewModel)
    {
        this.InitializeComponent();
        this.DataContext = viewModel;
    }
}
