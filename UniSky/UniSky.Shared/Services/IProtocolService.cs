using FishyFlip;

namespace UniSky.Services
{
    public interface IProtocolService
    {
        ATProtocol Protocol { get; }

        void SetProtocol(ATProtocol protocol);
    }
}