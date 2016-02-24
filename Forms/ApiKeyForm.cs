using System;
using System.Windows.Forms;

namespace WakaTime.Forms {
    public partial class ApiKeyForm : Form {
        private readonly WakaTimeConfigFile wakaTimeConfigFile;

        public ApiKeyForm() {
            InitializeComponent();

            wakaTimeConfigFile = new WakaTimeConfigFile();
        }

        private void ApiKeyForm_Load(object sender, EventArgs e) {
            try {
                txtAPIKey.Text = wakaTimeConfigFile.ApiKey;
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnOk_Click(object sender, EventArgs e) {
            try {
                Guid apiKey;
                bool parse = GuidTryParse(txtAPIKey.Text.Trim(), out apiKey);
                if (parse) {
                    wakaTimeConfigFile.ApiKey = apiKey.ToString();
                    wakaTimeConfigFile.Save();
                    PluginMain.ApiKey = apiKey.ToString();
                    Logger.Log("API Key changed.");
                } else {
                    MessageBox.Show("Please enter valid Api Key.");
                    DialogResult = DialogResult.None; // do not close dialog box
                }
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
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
