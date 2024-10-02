using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Timers;
using System.Windows.Forms;

namespace folder_watch {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayIconApplicationContext());
        }
    }

    public class MonitorConfig {
        public List<FolderMonitor> FoldersToMonitor { get; set; }

    }

    public class FolderMonitor {
        public bool Enabled { get; set; }
        public string Path { get; set; }
        public MonitorType Type { get; set; }
        public DateTime LastCheckTime { get; set; }
        public string MarkerFileName { get; set; }
        public JobType JobType { get; set; }
        public string OutputPath { get; set; }
    }

    public enum MonitorType {
        FileChanges,
        MarkerFile
    }

    public enum JobType {
        Auto, EP, EP_SPLIT, JEP
    }

    public class TrayIconApplicationContext : ApplicationContext {
        private NotifyIcon trayIcon;
        private ContextMenuStrip contextMenu;
        private Form mainForm;
        private System.Timers.Timer folderCheckTimer;
        private List<FolderMonitor> foldersToMonitor;

        public TrayIconApplicationContext() {
            InitializeComponents();
            SetupFolderMonitors();
            SetupFolderWatcher();
        }

        private void InitializeComponents() {
            mainForm = new Form1();
            mainForm.FormClosing += MainForm_FormClosing;

            contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Open", null, OpenForm);
            contextMenu.Items.Add("Exit", null, Exit);

            trayIcon = new NotifyIcon() {
                Icon = SystemIcons.Application,
                ContextMenuStrip = contextMenu,
                Visible = true
            };

            trayIcon.Click += TrayIcon_Click;
        }

        private void SetupFolderMonitors() {
            foldersToMonitor = new List<FolderMonitor>
            {
                new FolderMonitor
                {
                    Path = @"C:\Path\To\Folder1",
                    Type = MonitorType.FileChanges,
                    LastCheckTime = DateTime.Now
                },
                new FolderMonitor
                {
                    Path = @"C:\Path\To\Folder2",
                    Type = MonitorType.MarkerFile,
                    MarkerFileName = "job.submit",
                    LastCheckTime = DateTime.Now
                }
                // Add more folders to monitor as needed
            };
        }

        private void SetupFolderWatcher() {
            folderCheckTimer = new System.Timers.Timer(5000); // Check every 5 seconds
            folderCheckTimer.Elapsed += FolderCheckTimer_Elapsed;
            folderCheckTimer.Start();
        }

        private void FolderCheckTimer_Elapsed(object sender, ElapsedEventArgs e) {
            foreach (var monitor in foldersToMonitor) {
                if (monitor.Type == MonitorType.FileChanges) {
                    CheckForFileChanges(monitor);
                } else if (monitor.Type == MonitorType.MarkerFile) {
                    CheckForMarkerFile(monitor);
                }
            }
        }

        private void CheckForFileChanges(FolderMonitor monitor) {
            if (!Directory.Exists(monitor.Path)) {
                Console.WriteLine($"The directory {monitor.Path} does not exist.");
                return;
            }

            var currentFiles = Directory.GetFiles(monitor.Path);
            var currentTime = DateTime.Now;

            foreach (var file in currentFiles) {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTime > monitor.LastCheckTime) {
                    ShowNotification($"File changed in {Path.GetFileName(monitor.Path)}: {Path.GetFileName(file)}");
                }
            }

            monitor.LastCheckTime = currentTime;
        }

        private void CheckForMarkerFile(FolderMonitor monitor) {
            if (!Directory.Exists(monitor.Path)) {
                Console.WriteLine($"The directory {monitor.Path} does not exist.");
                return;
            }

            var subDirectories = Directory.GetDirectories(monitor.Path);
            foreach (var subDir in subDirectories) {
                var markerFilePath = Path.Combine(subDir, monitor.MarkerFileName);
                if (File.Exists(markerFilePath)) {
                    var fileInfo = new FileInfo(markerFilePath);
                    if (fileInfo.LastWriteTime > monitor.LastCheckTime) {
                        ShowNotification($"Marker file found in {Path.GetFileName(subDir)}");
                        File.Delete(markerFilePath); // Optionally delete the marker file after processing
                    }
                }
            }

            monitor.LastCheckTime = DateTime.Now;
        }

        private void ShowNotification(string message) {
            trayIcon.BalloonTipTitle = "Folder Monitor Alert";
            trayIcon.BalloonTipText = message;
            trayIcon.ShowBalloonTip(3000);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing) {
                e.Cancel = true;
                mainForm.Hide();
            }
        }

        private void TrayIcon_Click(object sender, EventArgs e) {
            if ((e as MouseEventArgs).Button == MouseButtons.Left) {
                OpenForm(sender, e);
            }
        }

        private void OpenForm(object sender, EventArgs e) {
            if (mainForm.Visible) {
                mainForm.Focus();
            } else {
                mainForm.Show();
            }
        }

        private void Exit(object sender, EventArgs e) {
            folderCheckTimer.Stop();
            trayIcon.Visible = false;
            Application.Exit();
        }
    }

}
