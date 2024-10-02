using ensims.jess_client;
using ensims.jess_client.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace jess_client.Forms {

    public partial class FolderManager : Form {
        // private TrayIconApplicationContext appContext;
        private FolderMonitor selectedMonitor;

        public FolderManager() {
            InitializeComponent();
            LoadFolderList();
            updateEditorPanel();
            this.FormClosing += form_FormClosing;
        }

        public void LoadFolderList() {
            folderListBox.Items.Clear();
            foreach (var monitor in GlobalUtility.Config.WatchedFolders) {
                folderListBox.Items.Add($"{monitor.Path} ({monitor.Type})");
            }
        }

        private void addButton_Click(object sender, EventArgs e) {
            FolderMonitor monitor = new FolderMonitor {
                Enabled = true,
                Path = "",
                Type = MonitorType.Subfolders,
                MarkerFileName = "",
                LastCheckTime = DateTime.Now
            };
            GlobalUtility.Config.AddFolderMonitor(monitor);
            LoadFolderList();
            folderListBox.SelectedIndex = folderListBox.Items.Count - 1;
        }

        private void removeButton_Click(object sender, EventArgs e) {
            if (folderListBox.SelectedIndex != -1) {
                var monitor = GlobalUtility.Config.WatchedFolders[folderListBox.SelectedIndex];
                GlobalUtility.Config.RemoveFolderMonitor(monitor);
                LoadFolderList();
                folderListBox.SelectedIndex = -1;
                selectedMonitor = null;
                updateEditorPanel();
            }
        }

        private void folderListBox_SelectedIndexChanged(object sender, EventArgs e) {
            if (folderListBox.SelectedIndex != -1) {
                selectedMonitor = GlobalUtility.Config.WatchedFolders [folderListBox.SelectedIndex];
            }else {
                selectedMonitor = null;
            }
            updateEditorPanel();
        }

        private void updateEditorPanel () {
            if (selectedMonitor != null) {
                enabledCheckBox.Enabled = true;
                enabledCheckBox.Checked = selectedMonitor.Enabled;
                pathTextBox.Enabled = true;
                pathTextBox.Text = selectedMonitor.Path;
                browseButton.Enabled = true;
                typeComboBox.Enabled = true;
                typeComboBox.SelectedItem = selectedMonitor.Type.ToString();
                markerFileTextBox.Enabled = typeComboBox.SelectedItem.ToString() == "MarkerFile";
                markerFileTextBox.Text = selectedMonitor.MarkerFileName;
            } else {
                enabledCheckBox.Enabled = false;
                pathTextBox.Enabled = false;
                pathTextBox.Text = "";
                browseButton.Enabled = false;
                typeComboBox.Enabled = false;
                markerFileTextBox.Enabled = false;
                markerFileTextBox.Text = "";
            }
        }

        private void pathTextBox_TextChanged(object sender, EventArgs e) {
            if (selectedMonitor != null) {
                selectedMonitor.Path = pathTextBox.Text;
                LoadFolderList();
            }
        }

        private void typeComboBox_SelectedIndexChanged(object sender, EventArgs e) {
            if (selectedMonitor != null) {
                selectedMonitor.Type = (MonitorType)Enum.Parse(typeof(MonitorType), typeComboBox.SelectedItem.ToString());
                markerFileTextBox.Enabled = typeComboBox.SelectedItem.ToString() == "MarkerFile";
                LoadFolderList();
            }
        }

        private void markerFileTextBox_TextChanged(object sender, EventArgs e) {
            if (selectedMonitor != null) {
                selectedMonitor.MarkerFileName = markerFileTextBox.Text;
            }
        }

        private void browseButton_Click(object sender, EventArgs e) {
            using (var fbd = new FolderBrowserDialog()) {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath)) {
                    pathTextBox.Text = fbd.SelectedPath;
                }
            }
        }

        private void chkOnOff_CheckedChanged(object sender, EventArgs e) {
            FolderMonitor.MonitorOn = chkOnOff.Checked;
            chkOnOff.Text = chkOnOff.Checked ? "ON" : "OFF";
        }

        private void saveButton_Click(object sender, EventArgs e) {
            GlobalUtility.Config.PersistClientConfig(GlobalUtility.ConfigFilepath);
            this.Close();
        }

        private void enabledCheckBox_CheckedChanged(object sender, EventArgs e) {
            selectedMonitor.Enabled = enabledCheckBox.Checked;
        }

        private void form_FormClosing(object sender, FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing) {
                e.Cancel = true;
                this.Hide();
            }
        }

    }
}
