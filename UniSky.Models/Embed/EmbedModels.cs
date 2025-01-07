using System;
using System.Collections.Generic;
using System.Text;

namespace UniSky.Models.Embed;

public enum UriEmbedType
{
    Image,
    Video,
    Audio,
    Article
}
public record struct UriEmbedImage(string Url, string? Alt, int? Width, int? Height);
public record struct UriEmbedDetails(string Title, string Description, UriEmbedImage? Image);
