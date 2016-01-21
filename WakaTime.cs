using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WakaTime {
    class WakaTime {
        private static DateTime lastSentTime = DateTime.UtcNow;
        private static string lastSentFile;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        private static string _apiKey = null;
        public static string ApiKey {
            get { return _apiKey; }
            set {
                if (value.IsNullOrWhiteSpace() == false) {
                    _apiKey = value;
                    UpdateApiKey(value);
                }
            }
        }

        public delegate void DelegateSendFile(string fileName, string projectName, bool isWrite = false);

        public static string LoadApiKey() {
            StringBuilder keyValue = new StringBuilder(255);
            string configFilepath = Utilities.GetConfigFilePath();
            if (configFilepath.IsNullOrWhiteSpace() == false) {
                if (GetPrivateProfileString("settings", "api_key", "", keyValue, 255, configFilepath) > 0) {
                    _apiKey = keyValue.ToString();
                    return keyValue.ToString();
                }
            }

            return null;
        }

        public static void UpdateApiKey(string apiKey) {
            string configFilepath = Utilities.GetConfigFilePath();
            if (apiKey.IsNullOrWhiteSpace() == false) {
                WritePrivateProfileString("settings", "api_key", apiKey, configFilepath);
            }
        }

        // https://wakatime.com/help/misc/creating-plugin#handling-editor-events
        public static void FileChanged(string fileName, string projectName) {
            if (lastSentFile == fileName) { return; }

            Logger.Debug("Event: File Changed");
            ThreadedSendFile(fileName, projectName);
            lastSentFile = fileName;
        }

        public static void FileModified(string fileName, string projectName) {
            if (lastSentTime.AddMinutes(2) <= DateTime.UtcNow) { return; }

            Logger.Debug("Event: File Modified");
            ThreadedSendFile(fileName, projectName);
            lastSentTime = DateTime.UtcNow;
        }

        public static void FileSaved(string fileName, string projectName) {
            Logger.Debug("WakaTime Event: File Saved");
            ThreadedSendFile(fileName, projectName, true);
        }

        public static void ThreadedSendFile(string fileName, string projectName, bool isWrite = false) {
            DelegateSendFile del = new DelegateSendFile(SendFile);
            del.BeginInvoke(fileName, projectName, isWrite, null, null);
        }

        public static void SendFile(string fileName, string projectName, bool isWrite = false) {
            string arguments = "\"" + Utilities.GetCLI() + "\" --key=\"" + ApiKey + "\""
                                + " --file=\"" + fileName + "\""
                                + " --plugin=\"FlashDevelop/4 flashdevelop-wakatime/" + PluginMain.Version + "\"";

            if (!projectName.IsNullOrWhiteSpace()) {
                arguments = arguments + " --project=\"" + projectName + "\"";
            }

            if (isWrite) {
                arguments = arguments + " --write";
            }

            string python = Utilities.GetPython();
            ProcessStartInfo procInfo = new ProcessStartInfo();
            procInfo.UseShellExecute = false;
            procInfo.FileName = python;
            procInfo.CreateNoWindow = true;
            procInfo.Arguments = arguments;
            procInfo.RedirectStandardOutput = true;
            procInfo.RedirectStandardError = true;

            try {
                var proc = Process.Start(procInfo);
                Logger.Debug("UtilityManager sendFile : " + python + " " + arguments);
                string output = proc.StandardOutput.ReadToEnd();
                string error = proc.StandardError.ReadToEnd();
                if (output.Length > 0) { Logger.Debug("Output:" + output); }
                if (error.Length > 0) { Logger.Debug("Error:" + error); }
            } catch (InvalidOperationException ex) {
                Logger.Debug("UtilityManager sendFile : " + python + " " + arguments);
                Logger.Debug("UtilityManager sendFile : " + ex.Message);
            } catch (Exception ex) {
                Logger.Debug("UtilityManager sendFile : " + python + " " + arguments);
                Logger.Debug("UtilityManager sendFile : " + ex.Message);
            }
        }
    }
}
