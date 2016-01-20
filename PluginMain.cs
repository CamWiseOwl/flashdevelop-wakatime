using PluginCore;
using PluginCore.Managers;
using PluginCore.Controls;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ScintillaNet;

namespace WakaTime {
    public class PluginMain : IPlugin {
        private String pluginName = "WakaTime";
        private String pluginGuid = "418dfca2-2210-4ee0-b66a-4574611739bc";
        private String pluginHelp = "www.flashdevelop.org/community/";
        private String pluginDesc = "WakaTime is time tracking for Programmers";
        private String pluginAuth = "Cameron WiseOwl";
        private ApiKeyDialog dialog;
        private Image pluginImage;
        private UtilityManager utilityManager = UtilityManager.Instance;

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

        [Browsable(false)]
        public Object Settings {
            get { return new Object(); }
        }
        #endregion

        public void Initialize() {
            this.pluginImage = Properties.Resources.wakatime;
            this.dialog = new ApiKeyDialog();
            ToolStripMenuItem menu = (ToolStripMenuItem)PluginBase.MainForm.FindMenuItem("HelpMenu");
            menu.DropDownItems.Add(new ToolStripMenuItem(this.pluginName, this.pluginImage, new EventHandler(this.OpenPanel)));
            checkFormLoaded();
        }

        private void checkFormLoaded() {
            Timer timer = new Timer();
            timer.Interval = 100;
            timer.Tick += timer_Tick;
            timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e) {
            // If there's a better way, blame the lack of Docs. But this works pretty nicely.
            if (Application.OpenForms.Count > 0) {
                Timer timer = (Timer) sender;
                timer.Stop();
                timer.Dispose();

                new System.Threading.Thread(init_Thread).Start();
            }
        }

        public void init_Thread() {
            utilityManager.Initialize(this);
        }

        public void Dispose() {}

        public void HandleEvent(Object sender, NotifyEvent e, HandlingPriority prority) {
            Console.WriteLine("Handle event");
            string fileName = PluginBase.MainForm.CurrentDocument.FileName;
            switch (e.Type) {
                case EventType.FileSwitch: // https://wakatime.com/help/misc/creating-plugin#handling-editor-events File Changed
                    Logger.Instance.Log("WakaTime Event: File Changed");
                    // TODO: Send wakatime-cli command
                    break;

                case EventType.FileSave: // https://wakatime.com/help/misc/creating-plugin#handling-editor-events File Saved
                    Logger.Instance.Log("WakaTime Event: File Saved");
                    // TODO: Send wakatime-cli command
                    break;

            }
        }

        public void AddEventHandlers() {
            //EventManager.AddEventHandler(this, EventType.FileSwitch | EventType.FileSave);
            Logger.Instance.Log("ADDINGEVENTLISTENERS");
            UITools.Manager.OnCharAdded += new UITools.CharAddedHandler(onChar);
            UITools.Manager.OnTextChanged += new UITools.TextChangedHandler(onTextChanged);
        }

        private void onChar(ScintillaControl sender, int value) {
            Logger.Instance.Log("WakaTime Event: File Modified");
        }

        private void onTextChanged(ScintillaControl sender, int position, int length, int linesAdded) {
            Logger.Instance.Log("WakaTime Event: File Modified");
        }

        public void OpenPanel(Object sender = null, System.EventArgs e = null) {
            if (dialog.ShowDialog() == DialogResult.OK) {
                dialog.Hide();
                Logger.Instance.Log("WakaTime API key set to " + dialog.GetApiText());
                utilityManager.ApiKey = dialog.GetApiText();
            }
        }
    }

}
