using System;
using System.Windows.Forms;

namespace WakaTime.Forms {
    public partial class SettingsForm : Form {
        private readonly WakaTimeConfigFile wakaTimeConfigFile;
        internal event EventHandler ConfigSaved;

        public SettingsForm() {
            InitializeComponent();

            wakaTimeConfigFile = new WakaTimeConfigFile();
        }

        private void SettingsForm_Load(object sender, EventArgs e) {
            try {
                txtAPIKey.Text = wakaTimeConfigFile.ApiKey;
                txtProxy.Text = wakaTimeConfigFile.Proxy;
                chkDebugMode.Checked = wakaTimeConfigFile.Debug;
            } catch (Exception ex) {
                Logger.Error("Error when loading form SettingsForm:", ex);
                MessageBox.Show(ex.Message);
            }
        }

        private void btnOk_Click(object sender, EventArgs e) {
            try {
                Guid apiKey;
                bool parse = GuidTryParse(txtAPIKey.Text.Trim(), out apiKey);

                if (parse) {
                    wakaTimeConfigFile.ApiKey = apiKey.ToString();
                    wakaTimeConfigFile.Proxy = txtProxy.Text.Trim();
                    wakaTimeConfigFile.Debug = chkDebugMode.Checked;
                    wakaTimeConfigFile.Save();
                    OnConfigSaved();
                    Logger.Log("API Key changed.");
                } else {
                    MessageBox.Show(@"Please enter valid Api Key.");
                    DialogResult = DialogResult.None; // do not close dialog box
                }
            } catch (Exception ex) {
                Logger.Error("Error when saving data from SettingsForm:", ex);
                MessageBox.Show(ex.Message);
            }
        }

        protected virtual void OnConfigSaved() {
            EventHandler handler = ConfigSaved;
            if (handler != null) { handler(this, EventArgs.Empty); }
        }

        private void label2_Click(object sender, EventArgs e) {

        }

        public static bool GuidTryParse(string s, out Guid guid) {
            try {
                guid = new Guid(s);
                return true;
            } catch {
                guid = Guid.Empty;
                return false;
            }
        }
    }
}
