using System.ComponentModel;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.Foundation;
using Windows.Storage;

namespace UniSky.Services;

#nullable enable

[JsonSerializable(typeof(sbyte))]
[JsonSerializable(typeof(short))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(byte))]
[JsonSerializable(typeof(ushort))]
[JsonSerializable(typeof(uint))]
[JsonSerializable(typeof(ulong))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Size))]
[JsonSerializable(typeof(Rect))]
[JsonSerializable(typeof(Point))]
[JsonSerializable(typeof(Vector2))]
[JsonSerializable(typeof(Vector3))]
[JsonSerializable(typeof(Vector4))]
[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class SettingsJsonContext : JsonSerializerContext { }

public class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions Options
        = new JsonSerializerOptions { TypeInfoResolver = SettingsJsonContext.Default };
    private readonly ApplicationDataContainer Settings = ApplicationData.Current.LocalSettings;

    public event PropertyChangedEventHandler? SettingChanged;

    /// <summary>
    /// Determines whether a setting already exists.
    /// </summary>
    /// <param name="key">Key of the setting (that contains object).</param>
    /// <returns>True if a value exists.</returns>
    public bool KeyExists(string key)
    {
        return Settings.Values.ContainsKey(key);
    }

    /// <summary>
    /// Retrieves a single item by its key.
    /// </summary>
    /// <typeparam name="T">Type of object retrieved.</typeparam>
    /// <param name="key">Key of the object.</param>
    /// <param name="default">Default value of the object.</param>
    /// <returns>The TValue object.</returns>
    public T? Read<T>(string key, T? @default = default)
    {
        if (Settings.Values.TryGetValue(key, out var valueObj) && valueObj is string valueString)
        {
            return JsonSerializer.Deserialize<T>(valueString, Options);
        }

        return @default;
    }

    /// <inheritdoc />
    public bool TryRead<T>(string key, out T? value)
    {
        if (Settings.Values.TryGetValue(key, out var valueObj) && valueObj is string valueString)
        {
            if (typeof(T) == typeof(string) && !valueString.StartsWith('"'))
            {
                // :D
                value = (T)(object)valueString;
                return true;
            }

            value = JsonSerializer.Deserialize<T>(valueString, Options);
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc />
    public void Save<T>(string key, T value)
    {
        Settings.Values[key] = JsonSerializer.Serialize(value, Options);

        SettingChanged?.Invoke(this, new PropertyChangedEventArgs(key));
    }

    /// <inheritdoc />
    public bool TryDelete(string key)
    {
        return Settings.Values.Remove(key);
    }

    /// <inheritdoc />
    public void Clear()
    {
        Settings.Values.Clear();
    }
}

#nullable disable