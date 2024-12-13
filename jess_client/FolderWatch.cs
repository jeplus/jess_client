using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Timers;
using System.Linq;
using log4net;
using ensims.jess_client.Classes;
using System.Threading;
using jess_client.Forms;
using ensims.jess_client.Forms;

namespace ensims.jess_client {

    public class TrayIconApplicationContext : ApplicationContext {

        private static readonly ILog log = LogManager.GetLogger(typeof(TrayIconApplicationContext));
        private static readonly Mutex localmut = new Mutex();

        private Mutex singleton = null;
        private NotifyIcon trayIcon;
        private ContextMenuStrip contextMenu;
        private FolderManager mainForm;
        private System.Timers.Timer folderCheckTimer;
        private System.Timers.Timer configCheckTimer;
        //private List<FolderMonitor> foldersToMonitor = GlobalUtility.Config.WatchedFolders;
        private DateTime LastConfigTime = DateTime.Now;

        public TrayIconApplicationContext() {
            try {
                singleton = new Mutex(true, "JESS.Client.Tray.Singleton", out bool mutexCreated);
                bool mutexAvailable = false;
                if (! mutexCreated) {
                    // Wait for mutex
                    mutexAvailable = singleton.WaitOne(2000);
                }
                if (mutexAvailable) {
                    //SetupFolderMonitors();
                    InitializeComponents();
                    StartConfigWatcher();
                    StartFolderWatcher();
                    JESSProcess.StartAutoRetrievalTimer(10, "all");
                }else {
                    log.Error("Cannot confirm single instance.");
                    Application.Exit();
                }
            } catch (AbandonedMutexException) {
                // not sure what to do with this
                log.Error("Abandoned Mutex exception.");
                Application.Exit();
            }
        }

        private void InitializeComponents() {
            mainForm = new FolderManager(); // new FolderManagementForm(this);
            // mainForm.FormClosing += MainForm_FormClosing;

            contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Open settings", Properties.Resources.SettingsOutline_16x, OpenForm);
            contextMenu.Items.Add("Close monitor", Properties.Resources.Close_red_16x, Exit);

            trayIcon = new NotifyIcon() {
                // Icon = SystemIcons.Application,
                Icon = Properties.Resources.favicon,
                ContextMenuStrip = contextMenu,
                Visible = true
            };

            trayIcon.Click += TrayIcon_Click;
        }

        private void StartConfigWatcher () {
            configCheckTimer = new System.Timers.Timer(2000); // Check every 2 seconds
            configCheckTimer.Elapsed += ConfigCheckTimer_Elapsed;
            configCheckTimer.Start();
        }

        private void ConfigCheckTimer_Elapsed(object sender, ElapsedEventArgs e) {
            var fileInfo = new FileInfo(GlobalUtility.ConfigFilepath);
            if (fileInfo.LastWriteTime > LastConfigTime) {
                log.Debug("Config change detected.");
                GlobalUtility.Config = ClientConfig.ParseClientConfig(GlobalUtility.ConfigFilepath);
                LastConfigTime = DateTime.Now;
                //if (mainForm != null) {
                //    mainForm.LoadFolderList();
                //}
            }
        }

        private void StartFolderWatcher() {
            folderCheckTimer = new System.Timers.Timer(5000); // Check every 5 seconds
            folderCheckTimer.Elapsed += FolderCheckTimer_Elapsed;
            folderCheckTimer.Start();
        }

        private void FolderCheckTimer_Elapsed(object sender, ElapsedEventArgs e) {
            if (FolderMonitor.MonitorOn && localmut.WaitOne(10)) {
                bool changeDetected = false;
                foreach (var monitor in GlobalUtility.Config.WatchedFolders) {
                    if (monitor.Enabled) {
                        if (monitor.Type == MonitorType.Subfolders) {
                            changeDetected |= CheckForSubfolderChanges(monitor);
                        } else if (monitor.Type == MonitorType.MarkerFile) {
                            changeDetected |= CheckForMarkerFile(monitor);
                        }
                    }
                }
                if (changeDetected) GlobalUtility.Config.PersistClientConfig(GlobalUtility.ConfigFilepath);
                localmut.ReleaseMutex();
            }
        }

        private void CheckForFileChanges(FolderMonitor monitor) {
            if (!Directory.Exists(monitor.Path)) {
                log.Error($"The directory {monitor.Path} does not exist.");
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

        private bool CheckForSubfolderChanges(FolderMonitor monitor) {
            if (!Directory.Exists(monitor.Path)) {
                log.Error($"The directory {monitor.Path} does not exist.");
                return false;
            }
            bool hasMods = false;
            var currentSubfolders = new Dictionary<string, DateTime>();
            foreach (var dir in Directory.GetDirectories(monitor.Path, "*", SearchOption.TopDirectoryOnly)) {
                if (!dir.EndsWith(".output", StringComparison.OrdinalIgnoreCase)) {
                    currentSubfolders [dir] = Directory.GetLastWriteTime(dir);
                }
            }

            var previousSubfolders = monitor.PreviousSubfolders ?? new Dictionary<string, DateTime>();
            // Remove any ".output" folders from previous subfolders (in case they were added before this rule)
            previousSubfolders = previousSubfolders
                .Where(kvp => !kvp.Key.EndsWith(".output", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Check for new subfolders
            var newSubfolders = currentSubfolders.Keys.Except(previousSubfolders.Keys);
            foreach (string newSubfolder in newSubfolders) {
                log.Info($"New subfolder in {Path.GetFileName(monitor.Path)}: {Path.GetFileName(newSubfolder)}");
                StartUploadtoJESS(new List<string> { newSubfolder }, "Folder watch", Path.Combine(monitor.Path, newSubfolder, "output"));
                //ShowNotification($"New subfolder in {Path.GetFileName(monitor.Path)}: {Path.GetFileName(newSubfolder)}");
                hasMods = true;
            }

            // Check for modified subfolders
            //foreach (string subfolder in currentSubfolders.Keys.Intersect(previousSubfolders.Keys)) {
            //    if (currentSubfolders [subfolder] > previousSubfolders [subfolder]) {
            //        log.Info($"Subfolder modified in {Path.GetFileName(monitor.Path)}: {Path.GetFileName(subfolder)}");
            //        StartUploadtoJESS(new List<string> { subfolder }, "Folder watch", "output");
            //        //ShowNotification($"Subfolder modified in {Path.GetFileName(monitor.Path)}: {Path.GetFileName(subfolder)}");
            //        hasMods = true;
            //    }
            //}

            // Update the monitor's state
            monitor.LastCheckTime = DateTime.Now;
            monitor.PreviousSubfolders = currentSubfolders;
            return hasMods;
        }

        private bool CheckForMarkerFile(FolderMonitor monitor) {
            if (!Directory.Exists(monitor.Path)) {
                log.Error($"The directory {monitor.Path} does not exist.");
                return false;
            }

            bool hasMods = false;
            var markerFilePath = Path.Combine(monitor.Path, monitor.MarkerFileName);
            if (File.Exists(markerFilePath)) {
                var fileInfo = new FileInfo(markerFilePath);
                // if (fileInfo.LastWriteTime > monitor.LastCheckTime) {
                    log.Info($"Marker file found in {Path.GetFileName(monitor.Path)}");
                    File.Delete(markerFilePath); // Optionally delete the marker file after processing
                    StartUploadtoJESS(new List<string> { monitor.Path }, "Folder watch", monitor.Path + ".output");
                    hasMods = true;
                // }
            }

            monitor.LastCheckTime = DateTime.Now;
            return hasMods;
        }

        private void ShowNotification(string message) {
            //trayIcon.BalloonTipTitle = "Folder Monitor Alert";
            //trayIcon.BalloonTipText = message;
            trayIcon.ShowBalloonTip(15000, "JESS Client", message, ToolTipIcon.Info);
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
            configCheckTimer.Stop();
            JESSProcess.StopAutoRetrievalTimer();
            trayIcon.Visible = false;
            singleton.ReleaseMutex();
            Application.Exit();
        }

        /// <summary>
        /// If files/folder to be uploaded count greater than 0 then start the upload process.
        /// </summary>
        private void StartUploadtoJESS(List<string> items, string caption, string resultdir) {

            log.Debug($"Uploading job ({caption}) with files: {String.Join(", ", items)}");

            // First, check in
            if (!JESSProcess.CheckInTransaction()) {
                if (!GlobalUtility.LogonDialogShown) {
                    frmLogin frm = new frmLogin();
                    frm.ShowDialog();
                    if (frm != null) { frm.Dispose(); frm = null; }
                }
            } else {
                log.Info("Check-in successful. Session key has been updated.");
            }

            if (GlobalUtility.LoggedOn) {
                // Check upload size
                long totalSize = DirectorySizeCalculator.GetTotalSize(items);
                log.Debug($"Upload size is {DirectorySizeCalculator.FormatSize(totalSize)}");
                if (totalSize > 200 * 1000 * 1000) {
                    log.Warn($"Folder {items [0]} ({DirectorySizeCalculator.FormatSize(totalSize)}) is too large to upload.");
                    return;
                }

                //if (items.Count == 1 && Directory.Exists(items [0])) {
                //    var size = GlobalUtility.CalcFolderSize(items [0]);
                //    if (size > 200) {
                //        log.Warn($"Folder {items [0]} ({size}MB) is too large to upload.");
                //        return;
                //    }
                //}

                // Start submitting
                var formData = new Dictionary<string, string> {
                                    { "title", caption },
                                    { "desc", String.Join(" ", items) }
                                };
                // Prepare files to upload
                if (items != null && items.Count() > 0) {

                    List<FileInfo> files = new List<FileInfo> { };
                    if (items.Count() == 1 && Path.GetExtension(items.First()).Equals(".zip")) {
                        files.Add(new FileInfo(@items.First()));
                    } else {
                        string zipfile = Path.Combine(MainModule.AppDataStorage.GetAppDataPath(), "tosubmit.zip");
                        zipfile = GlobalUtility.CreateZipArchive(zipfile, items.ToList());
                        files.Add(new FileInfo(@zipfile));
                    }

                    // Upload and run
                    var result = JESSProcess.SubmitTransaction(files, formData);

                    if (result != null && result.Ok) {
                        ShowNotification($"Job submitted with id {result.Data}.");
                        //if (File.Exists(zipfile)) {
                        //    File.Delete(zipfile);
                        //}

                        // Save pending job record to config file
                        // string outdir = Path.GetFullPath(String.Join("-", items) + ".output");
                        string outdir = Path.GetFullPath(resultdir);
                        ClientConfig.PendingJobRecord job = new ClientConfig.PendingJobRecord(result.Data, null, outdir);
                        GlobalUtility.Config.PendingJobs.Add(job.Id.ToString(), job);
                        GlobalUtility.Config.PersistClientConfig(GlobalUtility.ConfigFilepath);

                        log.Info($"Submission successful. Job {job.Id.ToString()} has been added to the pending jobs list.");
                    } else {
                        MessageBox.Show($"Job submission failed: {result.Status}", "Message", MessageBoxButtons.OK);
                    }
                }
            } else {
                log.Info("Please log on first.");
            }
        }


    }

}
