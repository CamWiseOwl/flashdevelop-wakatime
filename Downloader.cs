using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace WakaTime {
    public class Downloader {
        public static void DownloadAndInstallCli() {
            Logger.Log("WakaTime CLI Missing, downloading.", true);

            string url = WakaTimeConstants.CliUrl;
            string destinationDir = WakaTimeConstants.UserConfigDir;

            // Check for proxy setting
            WebProxy proxy = PluginMain.GetProxy();

            string localZipFile = Path.Combine(destinationDir, "wakatime-cli.zip");

            WebClient client = new WebClient { Proxy = proxy };

            // Download wakatime cli
            client.DownloadFile(url, localZipFile);

            Logger.Log("Finished downloading wakatime cli.", true);

            // Extract wakatime cli zip file
            ExtractZipFile(localZipFile, destinationDir);

            try {
                File.Delete(localZipFile);
            } catch { /* ignored */ }
        }

        public static void DownloadAndInstallPython() {
            Logger.Log("No Python install detected. Downloading embedded version.", true);

            var url = PythonManager.PythonDownloadUrl;
            var destinationDir = WakaTimeConstants.UserConfigDir;

            // Check for proxy setting
            var proxy = PluginMain.GetProxy();

            var localFile = Path.Combine(destinationDir, "python.zip");

            var client = new WebClient { Proxy = proxy };

            // Download embeddable python
            client.DownloadFile(url, localFile);

            Logger.Log("Finished downloading python.");

            // Extract wakatime cli zip file
            ExtractZipFile(localFile, Path.Combine(destinationDir, "python"));

            Logger.Log(string.Format("Finished extracting python: {0}", Path.Combine(destinationDir, "python")), true);

            try {
                File.Delete(localFile);
            } catch { /* ignored */ }
        }

        public static void ExtractZipFile(string zipFile, string outputFolder) {
            FastZip fastZip = new FastZip();
            try {
                fastZip.ExtractZip(zipFile, outputFolder, null); // Will always overwrite if target filenames already exist
            } catch (Exception ex) {
                Logger.Error("Extracting Zip failed.", ex);
            }
        }
    }
}