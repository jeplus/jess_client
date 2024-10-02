using System;
using System.Windows.Forms;
using ensims.jess_client.Classes;
using System.Text.RegularExpressions;
using log4net;

namespace ensims.jess_client.Forms {


    public partial class frmLogin : Form {

        private static readonly ILog log = LogManager.GetLogger(typeof(frmLogin));

        #region FormEvents
        public frmLogin() {
            InitializeComponent();
        }
        private void frmLogin_Load(object sender, EventArgs e) {
            txtServerUrl.Text = GlobalUtility.Config.ServerUrl;
            cmdLogOn.Enabled = false;
            GlobalUtility.LogonDialogShown = true;
        }
        private void frmLogin_Closed(object sender, System.EventArgs e) {
            GlobalUtility.LogonDialogShown = false;
        }
        #endregion

        #region FormControlEvents
        private void chkConfirm_CheckedChanged(object sender, EventArgs e) {
            cmdLogOn.Enabled = chkConfirm.Checked;
        }
        private void txtUsername_TextChanged(object sender, EventArgs e) {

        }
        private void cmdServiceterms_Click(object sender, EventArgs e) {
            System.Diagnostics.Process.Start("https://app.ensims.com/legal/");
        }
        private void buttonRegister_Click(object sender, EventArgs e) {
            System.Diagnostics.Process.Start("https://app.ensims.com/");
        }
        private void cmdLogOn_Click(object sender, EventArgs e) {
            if (txtServerUrl.Text.Trim() == string.Empty) { MessageBox.Show("Enter valid Server url", "Message", MessageBoxButtons.OK); return; }
            if (txtEmail.Text.Trim() == string.Empty || ! Email_validation().IsMatch(txtEmail.Text.Trim())) { MessageBox.Show("Please an enter valid email address", "Message", MessageBoxButtons.OK); return; }
            if (txtPassword.Text.Trim() == string.Empty) { MessageBox.Show("Please enter a valid password", "Message", MessageBoxButtons.OK); return; }

            try {
                if (JESSProcess.LogOnTransaction(txtEmail.Text.Trim(), txtPassword.Text.Trim())) {
                    log.Info("Logged on successfully");
                    GlobalUtility.Config.PersistClientConfig(GlobalUtility.ConfigFilepath);
                    this.Close();
                }else {
                    log.Info("Logon unsuccessful");
                }
            } finally { }
        }
        #endregion

        private static Regex Email_validation() {
            string pattern = @"^(?!\.)(""([^""\r\\]|\\[""\r\\])*""|"
                + @"([-a-z0-9!#$%&'*+/=?^_`{|}~]|(?<!\.)\.)*)(?<!\.)"
                + @"@[a-z0-9][\w\.-]*[a-z0-9]\.[a-z][a-z\.]*[a-z]$";

            return new Regex(pattern, RegexOptions.IgnoreCase);
        }

    }

}
