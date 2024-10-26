using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UniSky.Models;

namespace UniSky.Helpers;

[JsonSerializable(typeof(SessionModel))]
[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class JsonContext : JsonSerializerContext
{
}
