using System;
using System.Globalization;
using System.IO;
using PluginCore.Managers;

namespace WakaTime {
    class Logger {
        private static Logger _instance;
        private static string filePath;
        private static StreamWriter sw = null;

        public static Logger Instance {
            get {
                if (_instance == null) {
                    _instance = new Logger();
                }
                return _instance;
            }
        }

        private Logger() {
            filePath = ConfigFileHelper.GetLogFilePath();
            if (!filePath.IsNullOrWhiteSpace()) {
                try {
                    sw = new StreamWriter(filePath);
                } catch (Exception ex) {
                    Log("WakaTime Log Error:" + ex.Message);
                }
            }
        }

        public void Log(string message, bool shouldTrace = true) {
            Console.WriteLine(message);
            if (sw != null) { sw.WriteLine(message); }
            if (shouldTrace) { TraceManager.Add(message); }
        }

    }
}
