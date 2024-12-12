using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UniSky.Models;
using Windows.ApplicationModel;

namespace UniSky;

public class Constants
{
    public static string Version
    {
        get
        {
            var gitSha = "";
            var versionedAssembly = typeof(LoginModel).Assembly;
            var attribute = versionedAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            int idx;
            if (attribute != null && (idx = attribute.InformationalVersion.IndexOf('+')) != -1)
            {
                gitSha = "-" + attribute.InformationalVersion.Substring(idx + 1);
            }

            var v = Package.Current.Id.Version;
            return $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}{gitSha}";
        }
    }

    public static string UserAgent
        => $"UniSky/{Version} (https://github.com/UnicordDev/UniSky)";
}
