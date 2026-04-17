using System.Globalization;
using FishyFlip.Models;
using Windows.ApplicationModel.Resources;

namespace UniSky.ViewModels.Profile
{
    internal static class ProfileHelpers
    {
        private static readonly IdnMapping mapper = new IdnMapping();
        private static readonly ResourceLoader strings = ResourceLoader.GetForViewIndependentUse();

        public static string ConvertHandle(ATHandle handle, bool forDisplayName = false)
        {
            if (string.IsNullOrWhiteSpace(handle.Handle) || handle.Handle == "handle.invalid")
                return strings.GetString("Profile_InvalidHandle");

            return forDisplayName ? ConvertHandleString(handle) : $"@{ConvertHandleString(handle)}";
        }

        private static string ConvertHandleString(ATHandle handle)
        {
            try
            {
                return mapper.GetUnicode(handle.Handle);
            }
            catch
            {
                return handle.Handle;
            }
        }
    }
}