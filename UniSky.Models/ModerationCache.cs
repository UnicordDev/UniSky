using System;
using System.Collections.Generic;
using System.Text;
using UniSky.Moderation;

namespace UniSky.Models;

public record ModerationCache(DateTimeOffset SavedAt, ModerationOptions Options);