using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
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
[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class SettingsJsonContext : JsonSerializerContext { }

internal class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions Options
        = new JsonSerializerOptions { TypeInfoResolver = SettingsJsonContext.Default };

    /// <summary>
    /// Gets the settings container.
    /// </summary>
    public readonly ApplicationDataContainer Settings = ApplicationData.Current.LocalSettings;

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

    /// <summary>
    /// Determines whether a setting already exists in composite.
    /// </summary>
    /// <param name="compositeKey">Key of the composite (that contains settings).</param>
    /// <param name="key">Key of the setting (that contains object).</param>
    /// <returns>True if a value exists.</returns>
    public bool KeyExists(string compositeKey, string key)
    {
        if (TryRead(compositeKey, out ApplicationDataCompositeValue? composite) && composite != null)
        {
            return composite.ContainsKey(key);
        }

        return false;
    }

    /// <summary>
    /// Attempts to retrieve a single item by its key in composite.
    /// </summary>
    /// <typeparam name="T">Type of object retrieved.</typeparam>
    /// <param name="compositeKey">Key of the composite (that contains settings).</param>
    /// <param name="key">Key of the object.</param>
    /// <param name="value">The value of the object retrieved.</param>
    /// <returns>The T object.</returns>
    public bool TryRead<T>(string compositeKey, string key, out T? value)
    {
        if (TryRead(compositeKey, out ApplicationDataCompositeValue? composite) && composite != null)
        {
            string compositeValue = (string)composite[key];
            if (compositeValue != null)
            {
                value = JsonSerializer.Deserialize<T>(compositeValue, Options);
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Retrieves a single item by its key in composite.
    /// </summary>
    /// <typeparam name="T">Type of object retrieved.</typeparam>
    /// <param name="compositeKey">Key of the composite (that contains settings).</param>
    /// <param name="key">Key of the object.</param>
    /// <param name="default">Default value of the object.</param>
    /// <returns>The T object.</returns>
    public T? Read<T>(string compositeKey, string key, T? @default = default)
    {
        if (TryRead(compositeKey, out ApplicationDataCompositeValue? composite) && composite != null)
        {
            if (composite.TryGetValue(key, out object valueObj) && valueObj is string value)
            {
                return JsonSerializer.Deserialize<T>(value, Options);
            }
        }

        return @default;
    }

    /// <summary>
    /// Saves a group of items by its key in a composite.
    /// This method should be considered for objects that do not exceed 8k bytes during the lifetime of the application
    /// and for groups of settings which need to be treated in an atomic way.
    /// </summary>
    /// <typeparam name="T">Type of object saved.</typeparam>
    /// <param name="compositeKey">Key of the composite (that contains settings).</param>
    /// <param name="values">Objects to save.</param>
    public void Save<T>(string compositeKey, IDictionary<string, T> values)
    {
        if (TryRead(compositeKey, out ApplicationDataCompositeValue? composite) && composite != null)
        {
            foreach (KeyValuePair<string, T> setting in values)
            {
                if (composite.ContainsKey(setting.Key))
                {
                    composite[setting.Key] = JsonSerializer.Serialize(setting.Value, Options);
                }
                else
                {
                    composite.Add(setting.Key, JsonSerializer.Serialize(setting.Value, Options));
                }
            }
        }
        else
        {
            composite = new ApplicationDataCompositeValue();
            foreach (KeyValuePair<string, T> setting in values)
            {
                composite.Add(setting.Key, JsonSerializer.Serialize(setting.Value, Options));
            }

            Settings.Values[compositeKey] = composite;
        }
    }

    /// <summary>
    /// Deletes a single item by its key in composite.
    /// </summary>
    /// <param name="compositeKey">Key of the composite (that contains settings).</param>
    /// <param name="key">Key of the object.</param>
    /// <returns>A boolean indicator of success.</returns>
    public bool TryDelete(string compositeKey, string key)
    {
        if (TryRead(compositeKey, out ApplicationDataCompositeValue? composite) && composite != null)
        {
            return composite.Remove(key);
        }

        return false;
    }
}

#nullable disable