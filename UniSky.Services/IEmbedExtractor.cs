using System;
using System.Threading;
using System.Threading.Tasks;
using UniSky.Models.Embed;

namespace UniSky.Services;

public interface IEmbedExtractor
{
    Task<UriEmbedDetails?> ExtractEmbedAsync(Uri uri, CancellationToken cancellationToken = default);
}