using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using UniSky.Helpers;
using UniSky.Models;
using Windows.Storage;

namespace UniSky.Services;

public class SessionService : ISessionService
{
    private const string COMPOSITE_KEY = "UniSky_Sessions_v2";

    private readonly ApplicationDataContainer container
        = ApplicationData.Current.LocalSettings;

    public void SaveSession(SessionModel session)
    {
        var sessionString = JsonSerializer.Serialize(session, JsonContext.Default.SessionModel);
        var composite = GetContainer();

        composite[session.DID] = sessionString;
        container.Values[COMPOSITE_KEY] = composite;
    }

    public bool TryFindSession(string did, out SessionModel session)
    {
        session = null;

        var composite = GetContainer();
        if (!composite.TryGetValue(did, out var sessionStringObj)
            || sessionStringObj is not string sessionString)
            return false;

        try
        {
            session = JsonSerializer.Deserialize(sessionString, JsonContext.Default.SessionModel);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public IEnumerable<SessionModel> EnumerateAllSessions()
    {
        var composite = GetContainer();
        foreach (var item in composite)
        {
            if (TryFindSession(item.Key, out var session))
                yield return session;
        }
    }

    private ApplicationDataCompositeValue GetContainer()
    {
        if (!container.Values.TryGetValue(COMPOSITE_KEY, out var composite)
            || composite is not ApplicationDataCompositeValue value)
            value = [];
        return value;
    }
}
