using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FishyFlip.Lexicon;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Embed;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Lexicon.Com.Atproto.Repo;
using FishyFlip.Models;
using FishyFlip.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UniSky.Extensions;
using UniSky.Helpers;
using UniSky.Helpers.Interop;
using UniSky.Services;
using UniSky.ViewModels.Posts;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

namespace UniSky.ViewModels.Compose;

public partial class ComposeViewModel : ViewModelBase
{
    private static readonly string[] IMAGE_FILE_EXTENSIONS =
        [".jpg", ".jpeg", ".jfif", ".png", ".gif", ".webp", ".avif", ".tiff"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanPost))]
    [NotifyPropertyChangedFor(nameof(Characters))]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    private string _text;
    [ObservableProperty]
    private string _avatarUrl;
    [ObservableProperty]
    private int maxCharacters;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasReply))]
    private PostViewModel replyTo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanPost))]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    private ComposeViewLinkCardViewModel attachedUri;

    private readonly ResourceLoader resources;
    private readonly IProtocolService protocolService;
    private readonly IImageCompressionService compressionService;
    private readonly ILogger<ComposeViewModel> logger;

    // TODO: this but better
    public bool IsDirty
        => (!string.IsNullOrEmpty(Text) || HasAttachments || AttachedUri != null);
    // TODO: ditto
    public bool CanPost
        => (!string.IsNullOrEmpty(Text) || HasAttachments || AttachedUri != null) &&
            Text.Length <= 300 &&
            AttachedUri?.IsLoading != true &&
            !AttachedFiles.Any(a => a.IsLoading || a.IsErrored);

    public int Characters
        => Text?.Length ?? 0;
    public bool HasReply
        => ReplyTo != null;

    public ObservableCollection<ComposeViewAttachmentViewModel> AttachedFiles { get; }

    public bool HasAttachments
        => AttachedFiles.Count > 0;

    public IOverlayController SheetController { get; }

    public ComposeViewModel(IOverlayController sheetController,
                            IProtocolService protocolService,
                            IImageCompressionService compressionService,
                            ILogger<ComposeViewModel> logger,
                            PostViewModel replyTo = null)
    {
        this.protocolService = protocolService;
        this.logger = logger;
        this.SheetController = sheetController;
        this.compressionService = compressionService;
        this.resources = ResourceLoader.GetForCurrentView();

        this.Text = "";
        this.ReplyTo = replyTo;
        this.MaxCharacters = 300;
        this.AttachedFiles = [];
        this.AttachedFiles.CollectionChanged += (o, ev) =>
        {
            this.SetErrored(null);
            this.OnPropertyChanged(nameof(HasAttachments));
            this.OnPropertyChanged(nameof(CanPost));
        };
        Task.Run(LoadAsync);
    }

    partial void OnTextChanged(string value)
    {
        if (AttachedFiles != null && AttachedFiles.Count != 0)
            return;

        Uri attachedUri = null;
        var matches = Regex.Matches(value, @"(https?://[^\s]+)");
        foreach (Match match in matches)
        {
            if (!Uri.TryCreate(match.Value, UriKind.Absolute, out var uri))
                continue;

            attachedUri = uri;
            break;
        }

        if (attachedUri != null)
        {
            if (AttachedUri != null)
            {
                if (AttachedUri.Url == attachedUri)
                    return;

                AttachedUri.Dispose();
            }

            AttachedUri = new ComposeViewLinkCardViewModel(this, attachedUri);
        }
        else
        {
            AttachedUri?.Dispose();
            AttachedUri = null;
        }
    }


    private async Task LoadAsync()
    {
        using var loading = this.GetLoadingContext();

        var protocol = protocolService.Protocol;

        try
        {
            var profile = (await protocol.GetProfileAsync(protocol.AuthSession.Session.Did)
                .ConfigureAwait(false))
                .HandleResult();

            AvatarUrl = profile.Avatar;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch user info!");
            this.SetErrored(ex);
        }
    }

    [RelayCommand]
    private async Task PostAsync()
    {
        this.SetErrored(null);
        using var ctx = this.GetLoadingContext();

        var protocol = protocolService.Protocol;

        try
        {
            var text = Text;
            // todo: text processing step, for now, normalise newlines
            text = text.Replace("\r\n", "\n")
                       .Replace('\r', '\n');

            var handles = FacetHelpers.HandlesForMentions(text);

            ProfileViewDetailed[] profiles = [];
            if (handles.Length > 0)
            {
                var feedProfiles = (await protocol.Actor.GetProfilesAsync(handles.Cast<ATIdentifier>().ToList())
                    .ConfigureAwait(false))
                    .HandleResult();

                profiles = [.. feedProfiles.Profiles];
            }

            var facets = FacetHelpers.Parse(text, profiles);
            var replyRef = await GetReplyDefAsync().ConfigureAwait(false);
            var embed = await CreateEmbedAsync().ConfigureAwait(false);

            var postModel = new Post(text, reply: replyRef, embed: embed, facets: [..facets]);
            var post = (await protocolService.Protocol.CreatePostAsync(postModel)
                .ConfigureAwait(false))
                .HandleResult();

            Text = "";
            syncContext.Post(async () =>
            {
                AttachedFiles.Clear();
                await Hide();
            });
        }
        catch (Exception ex)
        {
            this.SetErrored(ex);
        }
    }

    private async Task<ATObject> CreateEmbedAsync()
    {
        if (AttachedUri != null)
        {
            var card = AttachedUri;
            if (card.ThumbnailBitmap != null)
            {
                using var memoryStream = new InMemoryRandomAccessStream();

                var image = await compressionService.CompressSoftwareBitmapAsync(card.ThumbnailBitmap, memoryStream);
                memoryStream.Seek(0);

                using var content = new StreamContent(memoryStream.AsStream());
                content.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);

                var blob = (await protocolService.Protocol.UploadBlobAsync(content)
                    .ConfigureAwait(false))
                    .HandleResult();

                var external = new EmbedExternal()
                {
                    External = new External(card.Url.ToString(), card.Title, card.Description, blob.Blob)
                };

                return external;
            }

            return null;
        }
        else
        {
            return await UploadImageEmbedAsync()
                .ConfigureAwait(false);
        }
    }

    private async Task<ATObject> UploadImageEmbedAsync()
    {
        EmbedImages embed = null;
        foreach (var image in this.AttachedFiles.Where(f => f.AttachmentType == ComposeViewAttachmentType.Image))
        {
            var properties = await image.StorageFile.GetBasicPropertiesAsync()
                .AsTask().ConfigureAwait(false);

            if (properties.Size > 1_000_000)
            {
                var e = new InvalidOperationException("Attached image is too large!");
                image.SetErrored(e);
                throw e;// useless, bad, awful, ideally never happens
            }

            using var stream = await image.StorageFile.OpenStreamForReadAsync()
                .ConfigureAwait(false);
            using var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);

            var blob = (await protocolService.Protocol.UploadBlobAsync(content)
                .ConfigureAwait(false))
                .HandleResult();

            embed ??= new EmbedImages([]);
            embed.Images.Add(new Image(blob.Blob, image.AltText, new AspectRatio(image.Width, image.Height)));
        }

        return embed;
    }

    [RelayCommand]
    private async Task Hide()
    {
        await this.SheetController.TryHideSheetAsync();
    }

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        try
        {
            this.SetErrored(null);

            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                CommitButtonText = resources.GetString("UploadToBluesky"),
                ViewMode = PickerViewMode.Thumbnail
            };

            foreach (var type in IMAGE_FILE_EXTENSIONS)
                picker.FileTypeFilter.Add(type);

            var files = await picker.PickMultipleFilesAsync();
            foreach (var file in files)
            {
                AddFile(file, false);
            }
        }
        catch (Exception ex)
        {
            this.SetErrored(ex);
        }
    }

    [RelayCommand]
    private async Task TakePhotoAsync()
    {
        try
        {
            this.SetErrored(null);

            var picker = new CameraCaptureUI();
            picker.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            picker.PhotoSettings.AllowCropping = false;

            var file = await picker.CaptureFileAsync(CameraCaptureUIMode.Photo);
            AddFile(file, false);
        }
        catch (Exception ex)
        {
            this.SetErrored(ex);
        }
    }

    private async Task<ReplyRefDef> GetReplyDefAsync()
    {
        ReplyRefDef replyRef = null;
        if (ReplyTo == null)
            return replyRef;

        var replyPost = ReplyTo.View;
        var replyRecord = (await protocolService.Protocol.GetRecordAsync(replyPost.Uri.Did, replyPost.Uri.Collection, replyPost.Uri.Rkey, replyPost.Cid)
            .ConfigureAwait(false))
            .HandleResult();

        if (replyRecord.Value is not Post replyPostFetched)
            throw new InvalidOperationException(resources.GetString("E_ReplyToNonPost"));

        var replyPostReplyDef = replyPostFetched.Reply;

        replyRef = new ReplyRefDef()
        {
            Root = replyPostReplyDef?.Root ?? new StrongRef() { Uri = replyPost.Uri, Cid = replyPost.Cid },
            Parent = new StrongRef() { Uri = replyPost.Uri, Cid = replyPost.Cid }
        };

        return replyRef;
    }

    internal bool HandlePaste()
    {
        var dataPackageView = Clipboard.GetContent();
        if (dataPackageView.Contains(StandardDataFormats.StorageItems) ||
            dataPackageView.Contains(StandardDataFormats.Bitmap) ||
            dataPackageView.Contains("DeviceIndependentBitmapV5"))
        {
            _ = DoPasteAsync(dataPackageView);
            return true;
        }

        return false;
    }

    internal bool HandleDrop(DataPackageView dataPackageView)
    {
        if (dataPackageView.Contains(StandardDataFormats.StorageItems) ||
            dataPackageView.Contains(StandardDataFormats.Bitmap) ||
            dataPackageView.Contains("DeviceIndependentBitmapV5"))
        {
            _ = DoPasteAsync(dataPackageView);
            return true;
        }

        return false;
    }

    private async Task DoPasteAsync(DataPackageView dataPackageView)
    {
        try
        {
            this.SetErrored(null);
            if (dataPackageView.Contains("DeviceIndependentBitmapV5"))
            {
                var data = (IRandomAccessStream)await dataPackageView.GetDataAsync("DeviceIndependentBitmapV5");
                var file = await BitmapInterop.SaveBitmapToFileAsync(data);
                AddFile(file, true);

                return;
            }

            if (dataPackageView.Contains(StandardDataFormats.Bitmap))
            {
                var data = await dataPackageView.GetBitmapAsync();
                var file = await BitmapInterop.SaveBitmapToFileAsync(data);
                AddFile(file, true);

                return;
            }

            if (dataPackageView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await dataPackageView.GetStorageItemsAsync();
                foreach (var file in items.OfType<IStorageFile>())
                    AddFile(file, false);

                return;
            }
        }
        catch (Exception ex)
        {
            this.SetErrored(ex);
        }
    }

    private void AddFile(IStorageFile storageFile, bool isTemporary)
    {
        if (storageFile == null) return;

        if (AttachedUri != null)
            throw new InvalidOperationException("A link is attached to this post!");

        if (storageFile is IStorageFilePropertiesWithAvailability properties && !properties.IsAvailable)
            throw new InvalidOperationException(resources.GetString("E_FileUnavailable"));

        // TODO: may not always cover webp/avif. dig into this.
        var type = storageFile.ContentType.StartsWith("image/") ? ComposeViewAttachmentType.Image :
                   storageFile.ContentType.StartsWith("video") ? ComposeViewAttachmentType.Video :
                   throw new InvalidOperationException(resources.GetString("E_NonImageOrVideo"));

        if (type == ComposeViewAttachmentType.Image)
        {
            if (AttachedFiles.Any(t => t.AttachmentType == ComposeViewAttachmentType.Video))
                throw new InvalidOperationException(resources.GetString("E_UnableToAddImageToVideoPost"));

            if (AttachedFiles.Where(t => t.AttachmentType == ComposeViewAttachmentType.Image).Count() + 1 > 4)
                throw new InvalidOperationException(resources.GetString("E_TooManyPhotos"));
        }

        if (type == ComposeViewAttachmentType.Video)
        {
            if (AttachedFiles.Any(t => t.AttachmentType == ComposeViewAttachmentType.Image))
                throw new InvalidOperationException(resources.GetString("E_UnableToAddVideoToImagePost"));

            if (AttachedFiles.Where(t => t.AttachmentType == ComposeViewAttachmentType.Video).Count() + 1 > 1)
                throw new InvalidOperationException(resources.GetString("E_TooManyVideos"));

            throw new InvalidOperationException(resources.GetString("E_VideosUnsupported"));
        }

        AttachedFiles.Add(ActivatorUtilities.CreateInstance<ComposeViewAttachmentViewModel>(ServiceContainer.Scoped, this, storageFile, type, isTemporary));
    }

    internal void UpdateLoading(bool value)
    {
        OnPropertyChanged(nameof(CanPost));
    }
}
