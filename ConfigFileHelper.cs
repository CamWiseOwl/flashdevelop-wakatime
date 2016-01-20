using System;
using System.Text;
using System.Runtime.InteropServices;

namespace WakaTime {
    class ConfigFileHelper {
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        /// <summary>
        /// Retrive ApiKey from config file.
        /// </summary>
        /// <returns></returns>
        public static string GetApiKey() {
            StringBuilder keyValue = new StringBuilder(255);
            string configFilepath = GetConfigFilePath();
            if (configFilepath.IsNullOrWhiteSpace() == false) {
                if (GetPrivateProfileString("settings", "api_key", "", keyValue, 255, configFilepath) > 0) {
                    return keyValue.ToString();
                }
            }

            return null;
        }

        /// <summary>
        /// Update ApiKey file in config file
        /// </summary>
        /// <returns></returns>
        public static void UpdateApiKey(string apiKey) {
            string configFilepath = GetConfigFilePath();
            if (apiKey.IsNullOrWhiteSpace() == false) {
                WritePrivateProfileString("settings", "api_key", apiKey, configFilepath);
            }
        }

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
                string fileName = filePath + "\\.wakatime.log";

                return fileName;
            }

            return null;

        }
    }
}
