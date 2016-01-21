using PluginCore.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WakaTime {
    public static class Logger {

        // Writes message to Visual Studio Console
        public static void Debug(string message) {
            Console.WriteLine("WakaTime:" + message);
        }

        // Writes message to FlashDevelop Output panel
        public static void Output(string message) {
            TraceManager.Add("WakaTime:" + message);
        }

        // Does both VS Console and FD Output
        public static void Log(string message) {
            Debug(message);
            Output(message);
        }
    }
}
