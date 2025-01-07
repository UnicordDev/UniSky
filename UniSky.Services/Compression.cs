using System;
using System.Collections.Generic;
using System.Text;
using Windows.Storage;
using Windows.Storage.Streams;

namespace UniSky.Models;

public record class CompressedImageFile(int Width, int Height, string ContentType, StorageFile StorageFile);
public record class CompressedImageStream(int Width, int Height, string ContentType, IRandomAccessStream Stream);

