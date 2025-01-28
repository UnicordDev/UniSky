using System.ComponentModel;

namespace UniSky.Services;

#nullable enable

public interface ISettingsService
{
    event PropertyChangedEventHandler SettingChanged;
    void Clear();
    bool KeyExists(string key);
    T? Read<T>(string key, T? @default = default);
    void Save<T>(string key, T value);
    bool TryDelete(string key);
    bool TryRead<T>(string key, out T? value);
}

#nullable disable
