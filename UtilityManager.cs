using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace WakaTime {
    /// <summary>
    /// Singleton class to check plugin .
    /// </summary>
    class UtilityManager {
        static bool is64BitProcess = (IntPtr.Size == 8);
        static bool is64BitOperatingSystem = is64BitProcess || InternalCheckIsWow64();

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(
            [In] IntPtr hProcess,
            [Out] out bool wow64Process
        );
        
        private Process _process = new Process();
        private string _apiKey = null;
        private static UtilityManager _instance;
        private UtilityManager() { }
        
        public string ApiKey {
            get {
                return _apiKey;
            }
            set {
                if (value.IsNullOrWhiteSpace() == false) {
                    _apiKey = value;
                    ConfigFileHelper.UpdateApiKey(value);
                }
            }
        }
        
        public static UtilityManager Instance {
            get {
                if (_instance == null) {
                    _instance = new UtilityManager();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Check Python installed or not, APIKEY exists or not, Command line utility installed or not
        /// </summary>
        /// <returns></returns>
        public void Initialize(PluginMain main) {
            try {
                // Make sure python is installed
                if (!isPythonInstalled()) {
                    Logger.Instance.Log("WakaTime: Python not found. Downloading...");
                    string pythonDownloadUrl = "https://www.python.org/ftp/python/3.4.2/python-3.4.2.msi";
                    if (is64BitOperatingSystem) {
                        pythonDownloadUrl = "https://www.python.org/ftp/python/3.4.2/python-3.4.2.amd64.msi";
                    }
                    Downloader.DownloadPython(pythonDownloadUrl, null);
                } else {
                    Logger.Instance.Log("WakaTime: Python found at " + GetPython(), false);
                }

                if (!doesCLIExist()) {
                    Logger.Instance.Log("WakaTime: CLI not found. Downloading...");
                    Downloader.DownloadCLI("https://github.com/wakatime/wakatime/archive/master.zip", GetCLIDir());
                } else {
                    Logger.Instance.Log("WakaTime: Cli found at " + GetCLI(), false);
                }

                _apiKey = ConfigFileHelper.GetApiKey();

                if (_apiKey.IsNullOrWhiteSpace()) {
                    Logger.Instance.Log("WakaTime: API Key could not be found.");
                    main.OpenPanel(); // (then on OK add event listeners)
                } else {
                    // Else add the necessary WakaTime event listeners
                    Logger.Instance.Log("WakaTime: API Key found in userdir, is: " + _apiKey);
                    main.AddEventHandlers();
                }
            } catch(Exception ex) {
                Logger.Instance.Log("WakaTime init error: " + ex.Message);
            }
        }

        public void SendFile(string fileName, string projectName, bool isWrite, string visualStudioVersion) {
            string arguments = "\"" + GetCLI() + "\" --key=\"" + _apiKey + "\""
                                + " --file=\"" + fileName + "\""
                                + " --plugin=\"Flashdevelop/4 WakaTime/0.1.0\"";

            if (!projectName.IsNullOrWhiteSpace())
                arguments = arguments + " --project=\"" + projectName + "\"";

            if (isWrite)
                arguments = arguments + " --write";

            ProcessStartInfo procInfo = new ProcessStartInfo();
            procInfo.UseShellExecute = false;
            procInfo.FileName = GetPython();
            procInfo.CreateNoWindow = true;
            procInfo.Arguments = arguments;

            try {
                var proc = Process.Start(procInfo);
            } catch (InvalidOperationException ex) {
                Logger.Instance.Log("UtilityManager sendFile : " + GetPython() + " " + arguments, false);
                Logger.Instance.Log("UtilityManager sendFile : " + ex.Message, false);
            } catch (Exception ex) {
                Logger.Instance.Log("UtilityManager sendFile : " + GetPython() + " " + arguments, false);
                Logger.Instance.Log("UtilityManager sendFile : " + ex.Message, false);
            }
        }

        public static string GetCurrentDirectory() {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var path = Path.GetDirectoryName(assembly);

            return path;
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

        #region Python Utils

        public string GetPythonDir() {
            return GetCurrentDirectory() + "\\Python";
        }

        public string GetPython() {
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
                    Logger.Instance.Log("WakaTime: Get Python error " + ex.Message);
                }
            }
            return null;
        }

        private bool doesPythonExist() {
            if (File.Exists(GetPythonDir() + "\\pythonw.exe")) {
                return true;
            }
            return false;
        }

        private bool isPythonInstalled() {
            if (GetPython() != null) {
                return true;
            }
            return false;
        }

        #endregion

        #region CLI Utils

        private bool doesCLIExist() {
            if (File.Exists(GetCLI())) {
                return true;
            }
            return false;
        }

        public string GetCLI() {
            string cliPath = GetCurrentDirectory() + "\\wakatime" + "\\wakatime-master\\wakatime-cli.py";
            Logger.Instance.Log("CLI Path:" + cliPath, false);
            return cliPath;
        }

        #endregion

    }
}
