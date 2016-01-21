using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WakaTime {
    public partial class APIKeyForm : Form {
        public APIKeyForm() {
            InitializeComponent();
        }

        public string GetApiText() {
            return this.apiKeyTextBox.Text;
        }

        public void SetApiText(string apiKey) {
            this.apiKeyTextBox.Text = apiKey;
        }
    }
}
