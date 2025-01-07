using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using OwlCore;
using UniSky.Extensions;
using UniSky.Services;
using Windows.Graphics.Imaging;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace UniSky.ViewModels.Compose;

public partial class ComposeViewLinkCardViewModel : ViewModelBase, IDisposable
{
    private static readonly string DebounceKey
        = nameof(ComposeViewLinkCardViewModel) + "_" + nameof(GenerateEmbedAsync);

    private readonly ComposeViewModel parent;
    private readonly Task executingTask;
    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly IEmbedExtractor embedExtractor;
    private readonly IEmbedThumbnailGenerator thumbnailGenerator;
    private readonly CoreDispatcher dispatcher = Window.Current.Dispatcher;
    [ObservableProperty]
    private string title;
    [ObservableProperty]
    private string description;
    [ObservableProperty]
    private string source;
    [ObservableProperty]
    private object thumbnail;

    public Uri Url { get; }
    public SoftwareBitmap ThumbnailBitmap { get; private set; }

    public ComposeViewLinkCardViewModel(ComposeViewModel parent, Uri url)
    {
        this.Url = url;
        this.parent = parent;
        this.embedExtractor = ServiceContainer.Scoped.GetRequiredService<IEmbedExtractor>();
        this.thumbnailGenerator = ServiceContainer.Scoped.GetRequiredService<IEmbedThumbnailGenerator>();
        this.cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        this.executingTask = Task.Run(() => GenerateEmbedAsync(cancellationTokenSource.Token));
    }

    private async Task GenerateEmbedAsync(CancellationToken token)
    {
        using var loading = this.GetLoadingContext();

        try
        {
            if (!await Flow.Debounce(DebounceKey, TimeSpan.FromMilliseconds(500)))
                return;

            token.ThrowIfCancellationRequested();
            var embedDetails = await embedExtractor.ExtractEmbedAsync(this.Url, token);
            if (embedDetails == null)
                throw new Exception("No embed found!");

            Title = embedDetails.Value.Title;
            Description = embedDetails.Value.Description;
            Source = this.Url.Host;

            if (embedDetails.Value.Image == null)
                return;

            try
            {
                this.ThumbnailBitmap = await thumbnailGenerator.GenerateThumbnailAsync(embedDetails.Value);
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    var softwareSource = new SoftwareBitmapSource();
                    await softwareSource.SetBitmapAsync(ThumbnailBitmap);

                    Thumbnail = softwareSource;
                });
            }
            catch
            {

            }
        }
        catch (Exception ex)
        {
            this.SetErrored(ex);
            this.Remove();
        }
    }


    [RelayCommand]
    private void Remove()
    {
        if (this.parent.AttachedUri == this)
            this.parent.AttachedUri = null;
    }

    protected override void OnLoadingChanged(bool value)
    {
        base.OnLoadingChanged(value);

        syncContext.Post(() => parent.UpdateLoading(value));
    }

    public void Dispose()
    {
        this.cancellationTokenSource.Cancel();
    }
}
