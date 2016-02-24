using PluginCore;
using PluginCore.Managers;
using PluginCore.Controls;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ScintillaNet;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using Timer = System.Windows.Forms.Timer;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace WakaTime {
    // WakaTime plugin for FlashDevelop
    public class PluginMain : IPlugin {
        #region Required Properties
        private String pluginName = "WakaTime";
        private String pluginGuid = "418dfca2-2210-4ee0-b66a-4574611739bc";
        private String pluginHelp = "www.flashdevelop.org/community/";
        private String pluginDesc = "WakaTime is time tracking for Programmers";
        private String pluginAuth = "Cameron WiseOwl";

        public Int32 Api
        {
            get { return 1; }
        }

        public String Name
        {
            get { return this.pluginName; }
        }

        public String Guid
        {
            get { return this.pluginGuid; }
        }

        public String Author
        {
            get { return this.pluginAuth; }
        }

        public String Description
        {
            get { return this.pluginDesc; }
        }

        public String Help
        {
            get { return this.pluginHelp; }
        }

        [Browsable(false)]
        public Object Settings
        {
            get { return new Object(); }
        }
        #endregion

        // Extra FD Plugin properties
        private Image pluginImage;
        private Forms.ApiKeyForm pluginPanel;

        // Settings
        public static bool Debug = false;
        public static string ApiKey;
        public static string Proxy;

        private static Forms.SettingsForm settingsForm;
        private static WakaTimeConfigFile wakaTimeConfigFile;
        private static readonly PythonCliParameters PythonCliParameters = new PythonCliParameters();
        private static string lastFile;
        private static string solutionName = string.Empty;
        DateTime lastHeartbeat = DateTime.UtcNow.AddMinutes(-3);

        public void Dispose() { }

        public void Initialize() {
            // Plugin property setup
            this.pluginImage = Properties.Resources.wakatime;
            this.pluginPanel = new Forms.ApiKeyForm();

            // Append API Form to the Help menu
            ToolStripMenuItem menu = (ToolStripMenuItem)PluginBase.MainForm.FindMenuItem("ToolsMenu");
            menu.DropDownItems.Add(new ToolStripSeparator());
            menu.DropDownItems.Add(new ToolStripMenuItem(this.pluginName, this.pluginImage, new EventHandler(menuItemCallback)));

            // Interval to check mainform loaded
            // (Can't find a way to access FD's mainform events through the API, this is a workaround)
            Timer timer = new Timer();
            timer.Interval = 100;
            timer.Tick += checkFormLoaded;
            timer.Start();
        }

        private void checkFormLoaded(object sender, EventArgs e) {
            if (Application.OpenForms.Count > 0) {
                Timer timer = (Timer)sender;
                timer.Stop();
                timer.Dispose();

                new Thread(() => InitializeAsync()).Start();
            }
        }

        public void InitializeAsync() {
            Logger.Log("InitializeAsync");
            try {
                Logger.Log(string.Format("Initializing WakaTime v{0}", WakaTimeConstants.PluginVersion));

                // Settings Form
                settingsForm = new Forms.SettingsForm();
                settingsForm.ConfigSaved += settingsFormOnConfigSaved;

                // Load config file
                wakaTimeConfigFile = new WakaTimeConfigFile();
                getSettings();

                try {
                    // Make sure python is installed
                    if (!PythonManager.IsPythonInstalled()) {
                        Downloader.DownloadAndInstallPython();
                    }

                    if (!doesCliExist() || !isCliLatestVersion()) {
                        try {
                            Directory.Delete(Path.Combine(WakaTimeConstants.UserConfigDir, "wakatime-master"), true);
                        } catch { /* ignored */ }

                        Downloader.DownloadAndInstallCli();
                    }
                } catch (WebException ex) {
                    Logger.Error("Are you behind a proxy? Try setting a proxy in WakaTime Settings with format https://user:pass@host:port. Exception Traceback:", ex, true);
                }

                if (string.IsNullOrEmpty(ApiKey)) {
                    promptApiKey();
                }

                // Setup event handlers
                AddEventHandlers();

                Logger.Log(string.Format("WakaTime v{0} initialised", WakaTimeConstants.PluginVersion), true);
            } catch (Exception ex) {
                Logger.Error("Error initialising Wakatime", ex, true);
            }
        }

        #region Event Handling
        public void AddEventHandlers() {
            UITools.Manager.OnTextChanged += new UITools.TextChangedHandler(this.sciControlTextChanged);
            EventManager.AddEventHandler(this, EventType.FileSwitch | EventType.FileSave);
        }

        public void HandleEvent(Object sender, NotifyEvent e, HandlingPriority prority) {
            string fileName = PluginBase.MainForm.CurrentDocument.FileName;

            switch (e.Type) {
                case EventType.FileSwitch:
                    handleActivity(fileName, false);
                    break;

                case EventType.FileSave:
                    handleActivity(fileName, true);
                    break;

            }
        }

        private void sciControlTextChanged(ScintillaControl sci, Int32 position, Int32 length, Int32 linesAdded) {
            string fileName = PluginBase.MainForm.CurrentDocument.FileName;
            handleActivity(fileName, false);
        }
        #endregion

        #region Public Methods
        public static void SendHeartbeat(string fileName, bool isWrite) {
            PythonCliParameters.Key = ApiKey;
            PythonCliParameters.File = fileName;
            PythonCliParameters.Plugin = string.Format("{0}/{1} {2}/{3}", WakaTimeConstants.EditorName, WakaTimeConstants.EditorVersion, WakaTimeConstants.PluginName, WakaTimeConstants.PluginVersion);
            PythonCliParameters.IsWrite = isWrite;
            PythonCliParameters.Project = getProjectName();

            var pythonBinary = PythonManager.GetPython();
            if (pythonBinary != null) {
                var process = new RunProcess(pythonBinary, PythonCliParameters.ToArray());
                if (Debug) {
                    Logger.Log(string.Format("[\"{0}\", \"{1}\"]", pythonBinary, string.Join("\", \"", PythonCliParameters.ToArray(true))));
                    process.Run();
                    Logger.Log(string.Format("CLI STDOUT: {0}", process.Output));
                    Logger.Log(string.Format("CLI STDERR: {0}", process.Error));
                } else
                    process.RunInBackground();

                if (!process.Success)
                    Logger.Error(string.Format("Could not send heartbeat: {0}", process.Error));
            } else
                Logger.Error("Could not send heartbeat because python is not installed");
        }

        public static WebProxy GetProxy() {
            WebProxy proxy = null;

            try {
                string proxyStr = Proxy;

                // Regex that matches proxy address with authentication
                var regProxyWithAuth = new Regex(@"\s*(https?:\/\/)?([^\s:]+):([^\s:]+)@([^\s:]+):(\d+)\s*");
                var match = regProxyWithAuth.Match(proxyStr);

                if (match.Success) {
                    string username = match.Groups[2].Value;
                    string password = match.Groups[3].Value;
                    string address = match.Groups[4].Value;
                    string port = match.Groups[5].Value;

                    NetworkCredential credentials = new NetworkCredential(username, password);
                    string[] proxyArr = new string[2] { address, port };
                    proxy = new WebProxy(string.Join(":", proxyArr), true, null, credentials);

                    Logger.Log("A proxy with authentication will be used.");
                    return proxy;
                }

                // Regex that matches proxy address and port(no authentication)
                var regProxy = new Regex(@"\s*(https?:\/\/)?([^\s@]+):(\d+)\s*");
                match = regProxy.Match(proxyStr);

                if (match.Success) {
                    string address = match.Groups[2].Value;
                    Int32 port = int.Parse(match.Groups[3].Value);

                    proxy = new WebProxy(address, port);

                    Logger.Log("A proxy will be used.");
                    return proxy;
                }

                Logger.Log("No proxy will be used. It's either not set or badly formatted.");
            } catch (Exception ex) {
                Logger.Error("Exception while parsing the proxy string from WakaTime config file. No proxy will be used.", ex);
            }

            return proxy;
        }

        public static class CoreAssembly {
            static readonly Assembly Reference = typeof(CoreAssembly).Assembly;
            public static readonly Version Version = Reference.GetName().Version;
        }
        #endregion

        #region Private Methods
        private void handleActivity(string currentFile, bool isWrite) {
            Logger.Log("HandleActivity");
            if (currentFile == null) { return; }

            if (!isWrite && lastFile != null && !enoughTimePassed() && currentFile.Equals(lastFile)) { return; }

            new Thread(() => SendHeartbeat(currentFile, isWrite)).Start();

            lastFile = currentFile;
            lastHeartbeat = DateTime.UtcNow;
        }

        private bool enoughTimePassed() {
            return lastHeartbeat < DateTime.UtcNow.AddMinutes(-1);
        }

        private static string getProjectName() {
            if (PluginBase.CurrentProject != null) {
                return PluginBase.CurrentProject.Name;
            }

            return string.Empty;
        }

        private static bool doesCliExist() {
            return File.Exists(PythonCliParameters.Cli);
        }

        private static bool isCliLatestVersion() {
            var process = new RunProcess(PythonManager.GetPython(), PythonCliParameters.Cli, "--version");
            process.Run();

            if (process.Success) {
                var currentVersion = process.Error.Trim();
                Logger.Log(string.Format("Current wakatime-cli version is {0}", currentVersion));

                Logger.Log("Checking for updates to wakatime-cli...");
                var latestVersion = WakaTimeConstants.LatestWakaTimeCliVersion();

                if (currentVersion.Equals(latestVersion)) {
                    Logger.Log("wakatime-cli is up to date.");
                    return true;
                } else {
                    Logger.Log(string.Format("Found an updated wakatime-cli v{0}", latestVersion));
                }

            }
            return false;
        }

        private static void menuItemCallback(object sender, EventArgs e) {
            try {
                settingsPopup();
            } catch (Exception ex) {
                Logger.Error("MenuItemCallback", ex);
            }
        }

        private static void settingsPopup() {
            settingsForm.ShowDialog();
        }

        private void promptApiKey() {
            pluginPanel.ShowDialog();
        }

        private string getPluginVersion() {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }

        private static void settingsFormOnConfigSaved(object sender, EventArgs eventArgs) {
            wakaTimeConfigFile.Read();
            getSettings();
        }

        private static void getSettings() {
            ApiKey = wakaTimeConfigFile.ApiKey;
            Debug = wakaTimeConfigFile.Debug;
            Proxy = wakaTimeConfigFile.Proxy;
        }
        #endregion
    }

}
