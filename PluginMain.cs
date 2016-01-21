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

namespace WakaTime {
    // WakaTime plugin for FlashDevelop
    public class PluginMain : IPlugin {
        private String pluginName = "WakaTime";
        private String pluginGuid = "418dfca2-2210-4ee0-b66a-4574611739bc";
        private String pluginHelp = "www.flashdevelop.org/community/";
        private String pluginDesc = "WakaTime is time tracking for Programmers";
        private String pluginAuth = "Cameron WiseOwl";
        private static String pluginVers = "";
        private Image pluginImage;
        private APIKeyForm pluginPanel;

        #region Required Properties
        public Int32 Api {
            get { return 1; }
        }

        public String Name {
            get { return this.pluginName; }
        }

        public String Guid {
            get { return this.pluginGuid; }
        }

        public String Author {
            get { return this.pluginAuth; }
        }

        public String Description {
            get { return this.pluginDesc; }
        }

        public String Help {
            get { return this.pluginHelp; }
        }

        public static String Version {
            get { return pluginVers;  }
        }

        [Browsable(false)]
        public Object Settings {
            get { return new Object(); }
        }
        #endregion

        public void Dispose() { }
        public void Initialize() {
            // Run basic plugin stuff.
            this.pluginImage = Properties.Resources.wakatime;
            this.pluginPanel = new APIKeyForm();
            ToolStripMenuItem menu = (ToolStripMenuItem)PluginBase.MainForm.FindMenuItem("HelpMenu");
            menu.DropDownItems.Add(new ToolStripMenuItem(this.pluginName, this.pluginImage, new EventHandler(this.OpenPanel)));

            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            pluginVers = fvi.FileVersion;

            // Then check on interval for the FlashDevelop main form to load
            // (Is there a better way?)
            Timer timer = new Timer();
            timer.Interval = 100;
            timer.Tick += checkFormLoaded;
            timer.Start();
        }

        private void checkFormLoaded(object sender, EventArgs e) {
            if (Application.OpenForms.Count > 0) {
                Logger.Debug("FD Main form loaded");
                Timer timer = (Timer)sender;
                timer.Stop();
                timer.Dispose();

                new System.Threading.Thread(() => onFormLoaded()).Start();
            }
        }

        private void onFormLoaded() {
            // TODO: Check the Python check works
            Utilities.CheckPythonInstalled();
            Utilities.CheckCLIInstalled();
            WakaTime.LoadApiKey();

            if (WakaTime.ApiKey.IsNullOrWhiteSpace()) {
                OpenPanel(); // (then on OK add event listeners)
            } else {
                // Else add the necessary WakaTime event listeners
                Logger.Debug("API Key found, is: " + WakaTime.ApiKey);
                AddEventHandlers();
            }
        }

        public void HandleEvent(Object sender, NotifyEvent e, HandlingPriority prority) {
            string fileName = PluginBase.MainForm.CurrentDocument.FileName;
            string projectName = "";
            if (PluginBase.CurrentProject != null) {
                projectName = PluginBase.CurrentProject.Name;
            }

            switch (e.Type) {
                case EventType.FileSwitch:
                    WakaTime.FileChanged(fileName, projectName);
                    break;

                case EventType.FileSave:
                    WakaTime.FileSaved(fileName, projectName);
                    break;

            }
        }

        public void AddEventHandlers() {
            UITools.Manager.OnTextChanged += new UITools.TextChangedHandler(this.SciControlTextChanged);
            EventManager.AddEventHandler(this, EventType.FileSwitch | EventType.FileSave);
        }
        private void SciControlTextChanged(ScintillaControl sci, Int32 position, Int32 length, Int32 linesAdded) {
            string fileName = PluginBase.MainForm.CurrentDocument.FileName;
            string projectName = PluginBase.CurrentProject.Name;
            WakaTime.FileModified(fileName, projectName);
        }

        public void OpenPanel(Object sender = null, System.EventArgs e = null) {
            pluginPanel.SetApiText(WakaTime.ApiKey);

            if (pluginPanel.ShowDialog() == DialogResult.OK) {
                pluginPanel.Hide();
                Logger.Debug("API key set to " + pluginPanel.GetApiText());
                WakaTime.ApiKey = pluginPanel.GetApiText();
            }
        }
    }

}
