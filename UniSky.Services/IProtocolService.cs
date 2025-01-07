using System.Threading.Tasks;
using FishyFlip;
using UniSky.Models;

namespace UniSky.Services;

public interface IProtocolService
{
    ATProtocol Protocol { get; }

    Task RefreshSessionAsync(SessionModel sessionModel);
    void SetProtocol(ATProtocol protocol);
}