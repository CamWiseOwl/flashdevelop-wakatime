using System;
using System.Net;
using System.Text.RegularExpressions;
using EnvDTE;
using EnvDTE80;
using PluginCore;

namespace WakaTime {
    internal static class WakaTimeConstants {
        internal const string PluginName = "flashdevelop-wakatime";
        internal static string PluginVersion = string.Format("{0}.{1}.{2}", PluginMain.CoreAssembly.Version.Major, PluginMain.CoreAssembly.Version.Minor, PluginMain.CoreAssembly.Version.Build);
        internal const string EditorName = "flashdevelop";
        internal static string EditorVersion = PluginBase.MainForm.ProductVersion;
        internal const string CliUrl = "https://github.com/wakatime/wakatime/archive/master.zip";
        internal const string CliFolder = @"wakatime-master\wakatime\cli.py";
        internal static string UserConfigDir
        {
            get
            {
                return Environment.GetEnvironmentVariable("USERPROFILE");
            }
        }

        internal static Func<string> LatestWakaTimeCliVersion = () => {
            Regex regex = new Regex(@"(__version_info__ = )(\(( ?\'[0-9]+\'\,?){3}\))");

            WebClient client = new WebClient { Proxy = PluginMain.GetProxy() };

            try {
                string about = client.DownloadString("https://raw.githubusercontent.com/wakatime/wakatime/master/wakatime/__about__.py");
                Match match = regex.Match(about);

                if (match.Success) {
                    Group grp1 = match.Groups[2];
                    Regex regexVersion = new Regex("([0-9]+)");
                    MatchCollection match2 = regexVersion.Matches(grp1.Value);
                    return string.Format("{0}.{1}.{2}", match2[0].Value, match2[1].Value, match2[2].Value);
                } else {
                    Logger.Log("Couldn't auto resolve wakatime cli version");
                }
            } catch (Exception ex) {
                Logger.Error("Exception when checking current wakatime cli version: ", ex);
            }
            return string.Empty;
        };
    }
}
