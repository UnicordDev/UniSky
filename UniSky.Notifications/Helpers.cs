using Microsoft.Extensions.Caching.Memory;

namespace UniSky.Notifications;

public class Helpers
{
    public static Func<ICacheEntry, Task<T>> CacheForTime<T>(TimeSpan timeSpan, Func<ICacheEntry, Task<T>> func)
    {
        return entry =>
        {
            entry.SetAbsoluteExpiration(timeSpan);
            return func(entry);
        };
    }
}
