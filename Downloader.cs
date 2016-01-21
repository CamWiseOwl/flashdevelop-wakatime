using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace WakaTime {
    class Downloader {
        private static string pythonURL = "https://www.python.org/ftp/python/3.4.2/python-3.4.2.msi";
        private static string python64URL = "https://www.python.org/ftp/python/3.4.2/python-3.4.2.amd64.msi";
        private static string cliURL = "https://github.com/wakatime/wakatime/archive/master.zip";

        // Download the WakaTime CLI and Extract it
        public static void DownloadCLI() {
            string cliPath = Utilities.GetCLIPath();
            WebClient client = new WebClient();
            string fileToDownload = cliPath + "\\wakatime-cli.zip";

            Directory.CreateDirectory(cliPath);
            Logger.Log("Downloading cli, target: " + fileToDownload);
            try {
                client.DownloadFile(cliURL, fileToDownload);
            } catch (WebException ex) {
                Logger.Log("WebException: " + ex.Message);
                return;
            }

            Logger.Log("Extracting cli, target: " + cliPath);
            ExtractZipFile(fileToDownload, cliPath);
            Logger.Log("Cli extracted");
        }

        public static void ExtractZipFile(string zipFile, string outputFolder) {
            FastZip fastZip = new FastZip();

            // Will always overwrite if target filenames already exist
            fastZip.ExtractZip(zipFile, outputFolder, null);
        }

        // Download and install Python
        public static void DownloadPython(bool x64) {
            string fileToDownload = GetCurrentDirectory() + "\\python.msi";

            WebClient client = new WebClient();
            Logger.Log("Downloading python.msi");
            client.DownloadFile(x64 ? python64URL : pythonURL, fileToDownload);

            ProcessStartInfo procInfo = new ProcessStartInfo();
            procInfo.UseShellExecute = false;
            procInfo.RedirectStandardOutput = true;
            procInfo.RedirectStandardError = true;
            procInfo.FileName = "msiexec";
            procInfo.CreateNoWindow = true;
            procInfo.Arguments = "/i \"" + fileToDownload + "\" /norestart /qb!";

            Logger.Log("Installing python...");
            var proc = Process.Start(procInfo);
            Logger.Debug(proc.StandardOutput.ReadToEnd());
            Logger.Debug(proc.StandardError.ReadToEnd());
            Logger.Log("Finished installing python.");
        }

        public static string GetCurrentDirectory() {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var path = Path.GetDirectoryName(assembly);

            return path;
        }

        // Search command line utility from downloaded folder
        public static string SearchFile(string dir, string fileToSearch) {
            foreach (string subDir in Directory.GetDirectories(dir)) {
                foreach (string file in Directory.GetFiles(subDir, fileToSearch)) {
                    if (file.Contains(fileToSearch)) {
                        return file;
                    }
                }
                SearchFile(subDir, fileToSearch);
            }

            return null;
        }

        // Search 'wakatime' folder from downloaded folder
        public static string SearchFolder(string dir, string serachDir) {
            string[] directory = Directory.GetDirectories(dir, serachDir, SearchOption.AllDirectories);
            if (directory.Length > 0) {
                return directory[0];
            }

            return null;
        }

        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs) {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists) {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory doesn't exist, create it. 
            if (!Directory.Exists(destDirName)) {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files) {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location. 
            if (copySubDirs) {
                foreach (DirectoryInfo subdir in dirs) {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}
