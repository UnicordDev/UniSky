using System.Collections.Generic;
using System.Threading.Tasks;
using UniSky.Models;

namespace UniSky.Services;

public interface ISessionService
{
    IEnumerable<SessionModel> EnumerateAllSessions();
    void SaveSession(SessionModel session);
    bool TryFindSession(string did, out SessionModel session);
}