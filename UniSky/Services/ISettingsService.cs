using System.Collections.Generic;

namespace UniSky.Services;

#nullable enable
public interface ISettingsService
{
    void Clear();
    bool KeyExists(string key);
    bool KeyExists(string compositeKey, string key);
    T? Read<T>(string key, T? @default = default);
    T? Read<T>(string compositeKey, string key, T? @default = default);
    void Save<T>(string compositeKey, IDictionary<string, T> values);
    void Save<T>(string key, T value);
    bool TryDelete(string key);
    bool TryDelete(string compositeKey, string key);
    bool TryRead<T>(string key, out T? value);
    bool TryRead<T>(string compositeKey, string key, out T? value);
}

#nullable disable
