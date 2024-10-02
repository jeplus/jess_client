using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Timers;
using System.Linq;
using log4net;

namespace folder_watch1 {

        public class Program {
            [STAThread]
            static void Main() {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayIconApplicationContext());
            }
        }

        public class FolderMonitor {
            public string Path { get; set; }
            public MonitorType Type { get; set; }
            public DateTime LastCheckTime { get; set; }
            public string MarkerFileName { get; set; }
            public HashSet<string> PreviousFiles { get; set; }
            public Dictionary<string, DateTime> PreviousSubfolders { get; set; }
        }

        public enum MonitorType {
                FileChanges,
                MarkerFile
        }

        public class TrayIconApplicationContext : ApplicationContext {

            private static readonly ILog log = LogManager.GetLogger(typeof(TrayIconApplicationContext));

            private NotifyIcon trayIcon;
            private ContextMenuStrip contextMenu;
            private Form mainForm;
            private System.Timers.Timer folderCheckTimer;
            private List<FolderMonitor> foldersToMonitor;

            public TrayIconApplicationContext() {
                SetupFolderMonitors();
                InitializeComponents();
                SetupFolderWatcher();
            }

            private void InitializeComponents() {
                mainForm = new FolderManagementForm(this);
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
                foldersToMonitor = new List<FolderMonitor>();
            }

            public void AddFolderMonitor(FolderMonitor monitor) {
                foldersToMonitor.Add(monitor);
            }

            public void RemoveFolderMonitor(FolderMonitor monitor) {
                foldersToMonitor.Remove(monitor);
            }

            public List<FolderMonitor> GetFolderMonitors() {
                return foldersToMonitor;
            }

            private void SetupFolderWatcher() {
                folderCheckTimer = new System.Timers.Timer(5000); // Check every 5 seconds
                folderCheckTimer.Elapsed += FolderCheckTimer_Elapsed;
                folderCheckTimer.Start();
            }

            private void FolderCheckTimer_Elapsed(object sender, ElapsedEventArgs e) {
                foreach (var monitor in foldersToMonitor) {
                    if (monitor.Type == MonitorType.FileChanges) {
                        CheckForSubfolderChanges(monitor);
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

            var currentFiles = new HashSet<string>(Directory.GetFiles(monitor.Path));
            var previousFiles = monitor.PreviousFiles ?? new HashSet<string>();
            var currentTime = DateTime.Now;

            // Check for new files
            var newFiles = currentFiles.Except(previousFiles);
            foreach (var newFile in newFiles) {
                ShowNotification($"New file in {Path.GetFileName(monitor.Path)}: {Path.GetFileName(newFile)}");
            }

            // Check for modified files
            var existingFiles = currentFiles.Intersect(previousFiles);
            foreach (var file in existingFiles) {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTime > monitor.LastCheckTime) {
                    ShowNotification($"File modified in {Path.GetFileName(monitor.Path)}: {Path.GetFileName(file)}");
                }
            }

            // Check for deleted files
            var deletedFiles = previousFiles.Except(currentFiles);
            foreach (var deletedFile in deletedFiles) {
                ShowNotification($"File deleted from {Path.GetFileName(monitor.Path)}: {Path.GetFileName(deletedFile)}");
            }

            // Update the monitor's state
            monitor.LastCheckTime = currentTime;
            monitor.PreviousFiles = currentFiles;
        }

        private void CheckForSubfolderChanges(FolderMonitor monitor) {
            if (!Directory.Exists(monitor.Path)) {
                Console.WriteLine($"The directory {monitor.Path} does not exist.");
                return;
            }

            var currentSubfolders = new Dictionary<string, DateTime>();
            foreach (var dir in Directory.GetDirectories(monitor.Path, "*", SearchOption.TopDirectoryOnly)) {
                currentSubfolders [dir] = Directory.GetLastWriteTime(dir);
            }

            var previousSubfolders = monitor.PreviousSubfolders ?? new Dictionary<string, DateTime>();

            // Check for new subfolders
            var newSubfolders = currentSubfolders.Keys.Except(previousSubfolders.Keys);
            foreach (var newSubfolder in newSubfolders) {
                log.Info($"New subfolder in {Path.GetFileName(monitor.Path)}: {Path.GetFileName(newSubfolder)}");
                //ShowNotification($"New subfolder in {Path.GetFileName(monitor.Path)}: {Path.GetFileName(newSubfolder)}");
            }

            // Check for modified subfolders
            foreach (var subfolder in currentSubfolders.Keys.Intersect(previousSubfolders.Keys)) {
                if (currentSubfolders [subfolder] > previousSubfolders [subfolder]) {
                    log.Info($"Subfolder modified in {Path.GetFileName(monitor.Path)}: {Path.GetFileName(subfolder)}");
                    //ShowNotification($"Subfolder modified in {Path.GetFileName(monitor.Path)}: {Path.GetFileName(subfolder)}");
                }
            }

            // Update the monitor's state
            monitor.LastCheckTime = DateTime.Now;
            monitor.PreviousSubfolders = currentSubfolders;
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
                        log.Info($"Marker file found in {Path.GetFileName(subDir)}");
                        //ShowNotification($"Marker file found in {Path.GetFileName(subDir)}");

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

        public class FolderManagementForm : Form {
            private TrayIconApplicationContext appContext;
            private ListBox folderListBox;
            private Button addButton;
            private Button editButton;
            private Button removeButton;

            public FolderManagementForm(TrayIconApplicationContext context) {
                appContext = context;
                InitializeComponent();
                LoadFolderList();
            }

            private void InitializeComponent() {
                this.Text = "Folder Monitor Management";
                this.Size = new Size(400, 300);

                folderListBox = new ListBox {
                    Dock = DockStyle.Top,
                    Height = 200
                };

                addButton = new Button {
                    Text = "Add",
                    Dock = DockStyle.Bottom
                };
                addButton.Click += AddButton_Click;

                editButton = new Button {
                    Text = "Edit",
                    Dock = DockStyle.Bottom
                };
                editButton.Click += EditButton_Click;

                removeButton = new Button {
                    Text = "Remove",
                    Dock = DockStyle.Bottom
                };
                removeButton.Click += RemoveButton_Click;

                this.Controls.Add(folderListBox);
                this.Controls.Add(addButton);
                this.Controls.Add(editButton);
                this.Controls.Add(removeButton);
            }

            private void LoadFolderList() {
                folderListBox.Items.Clear();
                foreach (var monitor in appContext.GetFolderMonitors()) {
                    folderListBox.Items.Add($"{monitor.Path} ({monitor.Type})");
                }
            }

            private void AddButton_Click(object sender, EventArgs e) {
                using (var form = new FolderEditForm()) {
                    if (form.ShowDialog() == DialogResult.OK) {
                        appContext.AddFolderMonitor(form.FolderMonitor);
                        LoadFolderList();
                    }
                }
            }

            private void EditButton_Click(object sender, EventArgs e) {
                if (folderListBox.SelectedIndex != -1) {
                    var selectedMonitor = appContext.GetFolderMonitors() [folderListBox.SelectedIndex];
                    using (var form = new FolderEditForm(selectedMonitor)) {
                        if (form.ShowDialog() == DialogResult.OK) {
                            appContext.GetFolderMonitors() [folderListBox.SelectedIndex] = form.FolderMonitor;
                            LoadFolderList();
                        }
                    }
                }
            }

            private void RemoveButton_Click(object sender, EventArgs e) {
                if (folderListBox.SelectedIndex != -1) {
                    var selectedMonitor = appContext.GetFolderMonitors() [folderListBox.SelectedIndex];
                    appContext.RemoveFolderMonitor(selectedMonitor);
                    LoadFolderList();
                }
            }
        }

        public class FolderEditForm : Form {
            private TextBox pathTextBox;
            private ComboBox typeComboBox;
            private TextBox markerFileTextBox;
            private Button okButton;
            private Button cancelButton;

            public FolderMonitor FolderMonitor { get; private set; }

            public FolderEditForm(FolderMonitor monitor = null) {
                InitializeComponent();
                if (monitor != null) {
                    pathTextBox.Text = monitor.Path;
                    typeComboBox.SelectedItem = monitor.Type.ToString();
                    markerFileTextBox.Text = monitor.MarkerFileName;
                }
            }

            private void InitializeComponent() {
                this.Text = "Edit Folder Monitor";
                this.Size = new Size(300, 200);

                var pathLabel = new Label { Text = "Path:", Location = new Point(10, 10) };
                pathTextBox = new TextBox { Location = new Point(10, 30), Width = 260 };

                var typeLabel = new Label { Text = "Type:", Location = new Point(10, 60) };
                typeComboBox = new ComboBox {
                    Location = new Point(10, 80),
                    Width = 260,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                typeComboBox.Items.AddRange(new object [] { "FileChanges", "MarkerFile" });
                typeComboBox.SelectedIndexChanged += TypeComboBox_SelectedIndexChanged;

                var markerFileLabel = new Label { Text = "Marker File:", Location = new Point(10, 110) };
                markerFileTextBox = new TextBox { Location = new Point(10, 130), Width = 260, Enabled = false };

                okButton = new Button { Text = "OK", Location = new Point(10, 160), DialogResult = DialogResult.OK };
                okButton.Click += OkButton_Click;

                cancelButton = new Button { Text = "Cancel", Location = new Point(100, 160), DialogResult = DialogResult.Cancel };

                this.Controls.AddRange(new Control [] { pathLabel, pathTextBox, typeLabel, typeComboBox, markerFileLabel, markerFileTextBox, okButton, cancelButton });
            }

            private void TypeComboBox_SelectedIndexChanged(object sender, EventArgs e) {
                markerFileTextBox.Enabled = typeComboBox.SelectedItem.ToString() == "MarkerFile";
            }

            private void OkButton_Click(object sender, EventArgs e) {
                FolderMonitor = new FolderMonitor {
                    Path = pathTextBox.Text,
                    Type = (MonitorType)Enum.Parse(typeof(MonitorType), typeComboBox.SelectedItem.ToString()),
                    MarkerFileName = markerFileTextBox.Text,
                    LastCheckTime = DateTime.Now
                };
            }
        }
}
