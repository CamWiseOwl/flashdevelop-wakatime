using PluginCore.Managers;
using System;

namespace WakaTime {
    public static class Logger {
        public static void Error(string message, Exception ex = null, bool traceAnyway = false) {
            Log(string.Format("{0}: {1}", message, ex, traceAnyway));
        }

        public static void Log(string message, bool traceAnyway = false) {
            string dateString = DateTime.UtcNow.ToString("hh:mm:ss tt");
            string outputMessage = string.Format("[Wakatime {0}] {1}", dateString, message);

            Console.WriteLine(outputMessage);
            if (PluginMain.Debug || traceAnyway == true) {
                TraceManager.Add(outputMessage);
            }
        }
    }
}
