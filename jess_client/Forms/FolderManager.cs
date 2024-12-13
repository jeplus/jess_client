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
        private FolderMonitor selectedMonitor;
        private BindingList<FolderMonitor> bindingList;
        private BindingSource bindingSource;

        public FolderManager() {
            InitializeComponent();
            
            bindingList = new BindingList<FolderMonitor>(GlobalUtility.Config.WatchedFolders.ToList());
            bindingSource = new BindingSource();
            bindingSource.DataSource = bindingList;
            folderListBox.DataSource = bindingSource;
            folderListBox.DisplayMember = "Path";
            updateEditorPanel();
            this.FormClosing += form_FormClosing;
        }

        private void addButton_Click(object sender, EventArgs e) {
            using (var fbd = new FolderBrowserDialog()) {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath)) {
                    string folderName = fbd.SelectedPath;
                    
                    // Check if the path already exists
                    var existingMonitor = bindingList
                        .FirstOrDefault(m => m.Path.Equals(folderName, StringComparison.OrdinalIgnoreCase));
                        
                    if (existingMonitor != null) {
                        folderListBox.SelectedItem = existingMonitor;
                        return;
                    }

                    FolderMonitor monitor = new FolderMonitor {
                        Enabled = true,
                        Path = folderName,
                        Type = MonitorType.MarkerFile,
                        MarkerFileName = "submit!",
                        LastCheckTime = DateTime.Now
                    };
                    
                    bindingList.Add(monitor);
                    folderListBox.SelectedItem = monitor;
                    
                    // Sync with WatchedFolders
                    GlobalUtility.Config.WatchedFolders.Clear();
                    foreach (var item in bindingList) {
                        GlobalUtility.Config.WatchedFolders.Add(item);
                    }
                }
            }
        }

        private void removeButton_Click(object sender, EventArgs e) {
            if (folderListBox.SelectedItem != null) {
                var monitor = (FolderMonitor)folderListBox.SelectedItem;
                bindingList.Remove(monitor);
                
                // Sync with WatchedFolders
                GlobalUtility.Config.WatchedFolders.Clear();
                foreach (var item in bindingList) {
                    GlobalUtility.Config.WatchedFolders.Add(item);
                }
                
                // Clear selection and update UI
                folderListBox.ClearSelected();
                selectedMonitor = null;
                updateEditorPanel();
            }
        }

        private void folderListBox_SelectedIndexChanged(object sender, EventArgs e) {
            selectedMonitor = folderListBox.SelectedItem as FolderMonitor;
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
            }
        }

        private void typeComboBox_SelectedIndexChanged(object sender, EventArgs e) {
            if (selectedMonitor != null) {
                selectedMonitor.Type = (MonitorType)Enum.Parse(typeof(MonitorType), typeComboBox.SelectedItem.ToString());
                markerFileTextBox.Enabled = typeComboBox.SelectedItem.ToString() == "MarkerFile";
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
