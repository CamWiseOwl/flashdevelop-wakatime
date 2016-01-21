using PluginCore.Managers;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace WakaTime {
    class Utilities {
        private static bool is64BitProcess = (IntPtr.Size == 8);
        private static bool is64BitOperatingSystem = is64BitProcess || InternalCheckIsWow64();

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(
            [In] IntPtr hProcess,
            [Out] out bool wow64Process
        );


        public static string GetConfigFilePath() {
            string cfgFilePath = Environment.GetEnvironmentVariable("USERPROFILE");
            if (cfgFilePath.IsNullOrWhiteSpace() == false) {
                string cfgfileName = cfgFilePath + "\\.wakatime.cfg";

                return cfgfileName;
            }

            return null;
        }

        public static string GetLogFilePath() {
            string filePath = Environment.GetEnvironmentVariable("USERPROFILE");
            if (filePath.IsNullOrWhiteSpace() == false) {
                return filePath + "\\.wakatime.log";
            }

            return null;
        }

        public static string GetCLIPath() {
            string filePath = Environment.GetEnvironmentVariable("APPDATA");
            if (filePath.IsNullOrWhiteSpace() == false) {
                return filePath + "\\WakaTime\\";
            }

            return null;
        }

        public static string GetCLI() {
            return GetCLIPath() + "\\wakatime-master\\wakatime\\cli.py";
        }

        // Make sure Python is installed.
        public static void CheckPythonInstalled() {
            if (GetPython() != null) {
                Log("WakaTime: Python found at " + GetPython(), false);
                return;
            }

            try {
                Downloader.DownloadPython(is64BitOperatingSystem);
            } catch (Exception ex) {
                Log("WakaTime: Python error: " + ex.Message);
            }
        }

        // Make Sure the CLI is installed
        public static void CheckCLIInstalled() {
            if (File.Exists(GetCLI()) == true) {
                Log("WakaTime: Cli found at " + GetCLI(), false);
                return;
            }

            Log("WakaTime: CLI not found. Downloading..." + GetCLI() + ", " + File.Exists(GetCLI()));
            Downloader.DownloadCLI();
        }

        // Logging method
        public static void Log(string message, bool shouldTrace = true) {
            Console.WriteLine(message);
            if (shouldTrace) { TraceManager.Add(message); }
        }

        // Is it 64 bit Windows?
        // http://stackoverflow.com/questions/336633/how-to-detect-windows-64-bit-platform-with-net
        public static bool InternalCheckIsWow64() {
            if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) || Environment.OSVersion.Version.Major >= 6) {
                using (Process p = Process.GetCurrentProcess()) {
                    bool retVal;
                    if (!IsWow64Process(p.Handle, out retVal)) {
                        return false;
                    }
                    return retVal;
                }
            } else {
                return false;
            }
        }

        public static string GetPython() {
            string[] locations = {
                "pythonw",
                "python",
                "\\Python37\\pythonw",
                "\\Python36\\pythonw",
                "\\Python35\\pythonw",
                "\\Python34\\pythonw",
                "\\Python33\\pythonw",
                "\\Python32\\pythonw",
                "\\Python31\\pythonw",
                "\\Python30\\pythonw",
                "\\Python27\\pythonw",
                "\\Python26\\pythonw",
                "\\python37\\pythonw",
                "\\python36\\pythonw",
                "\\python35\\pythonw",
                "\\python34\\pythonw",
                "\\python33\\pythonw",
                "\\python32\\pythonw",
                "\\python31\\pythonw",
                "\\python30\\pythonw",
                "\\python27\\pythonw",
                "\\python26\\pythonw",
                "\\Python37\\python",
                "\\Python36\\python",
                "\\Python35\\python",
                "\\Python34\\python",
                "\\Python33\\python",
                "\\Python32\\python",
                "\\Python31\\python",
                "\\Python30\\python",
                "\\Python27\\python",
                "\\Python26\\python",
                "\\python37\\python",
                "\\python36\\python",
                "\\python35\\python",
                "\\python34\\python",
                "\\python33\\python",
                "\\python32\\python",
                "\\python31\\python",
                "\\python30\\python",
                "\\python27\\python",
                "\\python26\\python",
            };
            foreach (string location in locations) {
                try {
                    ProcessStartInfo procInfo = new ProcessStartInfo();
                    procInfo.UseShellExecute = false;
                    procInfo.RedirectStandardError = true;
                    procInfo.FileName = location;
                    procInfo.CreateNoWindow = true;
                    procInfo.Arguments = "--version";
                    var proc = Process.Start(procInfo);
                    string errors = proc.StandardError.ReadToEnd();
                    if (errors == null || errors == "") {
                        return location;
                    }
                } catch (Exception ex) {
                    Log("WakaTime: Get Python error " + ex.Message);
                }
            }
            return null;
        }

    }
}
