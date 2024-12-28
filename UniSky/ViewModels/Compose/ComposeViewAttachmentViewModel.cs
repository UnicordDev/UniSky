using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FishyFlip.Tools;
using Microsoft.Extensions.Logging;
using UniSky.Controls.Compose;
using UniSky.Extensions;
using UniSky.Services;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace UniSky.ViewModels.Compose;

public enum ComposeViewAttachmentType
{
    Image, Video
}

public partial class ComposeViewAttachmentViewModel : ViewModelBase
{
    private readonly ComposeViewModel parent;
    private readonly ILogger<ComposeViewAttachmentViewModel> logger;
    private readonly IImageCompressionService imageCompressionService;

    [ObservableProperty]
    private BitmapImage thumbnail;
    [ObservableProperty]
    private string altText;

    public ComposeViewAttachmentType AttachmentType { get; private set; }
    public bool IsTemporary { get; private set; }
    public IStorageFile StorageFile { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public string ContentType { get; private set; }


    public ComposeViewAttachmentViewModel(ComposeViewModel parent,
                                          IStorageFile storageFile,
                                          ComposeViewAttachmentType type,
                                          bool isTemporary,
                                          IImageCompressionService imageCompressionService,
                                          ILogger<ComposeViewAttachmentViewModel> logger)
    {
        this.parent = parent;
        this.logger = logger;
        this.imageCompressionService = imageCompressionService;

        this.IsTemporary = isTemporary;
        this.StorageFile = storageFile;
        this.AttachmentType = type;
        this.ContentType = storageFile.ContentType;
        this.AltText = "";

        Task.Run(LoadAsync);
    }

    private async Task LoadAsync()
    {
        using var loading = this.GetLoadingContext();

        try
        {
            if (this.AttachmentType == ComposeViewAttachmentType.Image)
            {
                var oldFile = this.StorageFile;
                var compressedImage = await imageCompressionService.CompressStorageFileAsync(oldFile);

                if (IsTemporary)
                {
                    await TryDeleteTemporaryFile(oldFile);
                }

                Width = compressedImage.Width;
                Height = compressedImage.Height;
                ContentType = compressedImage.ContentType;
                StorageFile = compressedImage.StorageFile;
            }

            if (this.StorageFile is not IStorageItemProperties properties)
                return;

            var thumbStream = await properties.GetThumbnailAsync(ThumbnailMode.SingleItem, 512);
            syncContext.Post(async () =>
            {
                try
                {
                    Thumbnail ??= new BitmapImage();
                    await Thumbnail.SetSourceAsync(thumbStream);
                    thumbStream.Dispose();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to set thumbnail!");
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set thumbnail!");
        }
    }

    [RelayCommand]
    private void Remove()
    {
        this.parent.AttachedFiles.Remove(this);
    }

    [RelayCommand]
    private async Task AddAltTextAsync()
    {
        var previousAltText = AltText;
        var altTextDialog = new ComposeAddAltTextDialog(this);
        if (parent.SheetController != null && ApiInformation.IsApiContractPresent(typeof(UniversalApiContract).FullName, 8))
            altTextDialog.XamlRoot = parent.SheetController.Root.XamlRoot;

        if (await altTextDialog.ShowAsync() != ContentDialogResult.Primary)
            AltText = previousAltText;
    }

    protected override void OnLoadingChanged(bool value)
    {
        base.OnLoadingChanged(value);

        syncContext.Post(() => parent.UpdateLoading(value));
    }

    private async Task TryDeleteTemporaryFile(IStorageFile storageFile)
    {
        try
        {
            await storageFile.DeleteAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete temporary file.");
        }
    }

    public void SetErrored(Exception ex)
    {
        base.SetErrored(ex);
    }
}
