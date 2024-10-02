using System;
using System.Windows.Forms;
using System.Collections;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using ensims.jess_client.Forms;
using ensims.jess_client.Classes;
using log4net;
using CommandLine;
using CommandLine.Text;
using jess_client.Forms;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]


namespace ensims.jess_client {

    static class MainModule {

        private static readonly ILog log = LogManager.GetLogger(typeof(MainModule));

        // User32.dll is used to call GetForegroundWindow function which gives us the Active explorer window handle.
        [DllImport("User32.dll")]
        static extern int GetForegroundWindow();

        //[DllImport("user32.dll", SetLastError = false)]
        //static extern IntPtr GetDesktopWindow();

        //[DllImport("user32.dll")]
        //static extern int GetClassName(int hWnd, StringBuilder lpClassName, int nMaxCount);

        public class AppDataStorage {
            private static string appDataPath;

            static AppDataStorage() {
                // Get the path to the LocalApplicationData folder
                string basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                // Combine with your app name to create a unique folder
                appDataPath = Path.Combine(basePath, "jess_client");

                // Create the directory if it doesn't exist
                if (!Directory.Exists(appDataPath)) {
                    Directory.CreateDirectory(appDataPath);
                }
            }

            public static string GetAppDataPath() {
                return appDataPath;
            }

            public static void SaveData(string fileName, string content) {
                string filePath = Path.Combine(appDataPath, fileName);
                File.WriteAllText(filePath, content);
            }

            public static string LoadData(string fileName) {
                string filePath = Path.Combine(appDataPath, fileName);
                if (File.Exists(filePath)) {
                    return File.ReadAllText(filePath);
                }
                return null;
            }
        }

        public class Options {
            //[Option('h', "help", Required = false, HelpText = "Show commandline syntax")]
            //public bool Help { get; set; }

            [Option('f', "cfg", Required = false, HelpText = "Specify the configuration file")]
            public string Cfg { get; set; }

            [Option('x', "exitcode", Required = false, Default = 99, HelpText = "Return directly the exit code (for debugging purpose)")]
            public int ExitCode { get; set; }

            [Option('k', "checkin", Required = false, HelpText = "Check in to the online service with the exist key")]
            public bool CheckIn { get; set; }

            //[Option('g', "logon", Required = false, HelpText = "Prompt for logging on to the online service")]
            //public bool LogOn { get; set; }

            [Option('s', "submit", Required = false, HelpText = "Submit a job of the specified type: EP_IDF, EP_IDF_SPLIT or JE_JEP")]
            public string Submit { get; set; }

            [Option('d', "desc", Required = false, Separator = ':', HelpText = "Specify the title and the description of the submitted job. The fields are separated by ':'.")]
            public IEnumerable<string> Desc { get; set; }

            [Option('t', "options", Required = false, Separator = ':', HelpText = "Specify the job submission or retrieval options. Multiple fields are separated by ':'")]
            public IEnumerable<string> Option { get; set; }

            [Option('o', "output", Required = false, HelpText = "Specify the job's output folder for storing the results. This can be specified at submission or retrieving time.")]
            public string Output { get; set; }

            [Option('r', "retrieve", Required = false, HelpText = "Retrieve a job (with the id) or all pending jobs (with 'all'). Results are downloaded if simulation has completed, otherwise returns the job status")]
            public string Retrieve { get; set; }

            [Option('w', "watch", Required = false, HelpText = "Start folder watching and awaiting results")]
            public bool Watch { get; set; }

            [Option('c', "cancel", Required = false, Separator = ':', HelpText = "Cancel one or more pending jobs (with the id or ids separated by ':') or all pending jobs (with 'all'). Status of the job(s) will be returned")]
            public IEnumerable<string> Cancel { get; set; }

            [Value(0, HelpText = "File(s), zipped archives or folder(s) to submit")]
            public IEnumerable<string> Files { get; set; }

            [Usage(ApplicationAlias = "jess_client")]
            public static IEnumerable<Example> Examples {
                get {
                    yield return new Example("Validate current session", new Options { CheckIn = true });
                    yield return new Example("Submit an E+ job", new Options { Submit = "EP_IDF", Output = "../output", Files = new [] { "in.idf", "in.epw" } });
                    yield return new Example("Submit an accelerated E+ job", new Options { Submit = "EP_IDF", Output = "../output", Files = new [] { "in.idf", "in.epw" } });
                    yield return new Example("Submit a jE+ job", new Options { Submit = "JE_JEP", Option = new [] { "LHS", "10" }, Output = "../output", Files = new [] { "files.zip" } });
                    yield return new Example("Retrieve a job", new Options { Retrieve = "12345", Output = "../output" });
                    yield return new Example("Cancel a job", new Options { Cancel = new [] { "12345" } });
                }
            }
        }


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string [] args) {

            log.Debug("Commandline args: " + String.Join(", ", args));

            // If application is neither launched frpm context menu or comman line, exit the application.
            if (args.Length > 0) {

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                if (GlobalUtility.SelectedFilesFolder == null) { GlobalUtility.SelectedFilesFolder = new ArrayList(); }


                //1. If application is launced thriugh context menu.
                if (args [0].ToString().ToUpper().StartsWith("CONTEXTMENU")) {

                    GlobalUtility.Config = new ClientConfig();
                    // Load config
                    GlobalUtility.ConfigFilepath = AppDataStorage.GetAppDataPath() + "\\" + GlobalUtility.ConfigFilepath;
                    if (File.Exists(GlobalUtility.ConfigFilepath)) {
                        GlobalUtility.Config = ClientConfig.ParseClientConfig(GlobalUtility.ConfigFilepath);
                    }

                    try {
                        int handle = GetForegroundWindow();
                        List<string> selectedItems = new List<string> { };
                        foreach (SHDocVw.InternetExplorer window in new SHDocVw.ShellWindows()) {
                            if (window.HWND == (int)handle) {
                                string filename = Path.GetFileNameWithoutExtension(window.FullName).ToLower();
                                if (filename.ToLowerInvariant() == "explorer") {
                                    Shell32.FolderItems items = ((Shell32.IShellFolderViewDual2)window.Document).SelectedItems();
                                    foreach (Shell32.FolderItem item in items) {
                                        selectedItems.Add(item.Path);
                                    }
                                }
                            }
                        }

                        // selectedItems.Add("C:\\zyyz\\JESS_Client_v3.0.3_examples\\example_e+v95");

                        if (args [0].ToString().ToUpper().EndsWith("1")) {

                            StartUploadtoJESS(selectedItems, "Manual submission");

                        }else if (args [0].ToString().ToUpper().EndsWith("2")) {

                            // make sure no other instances of this app are running
                            try {
                                Mutex singleton = new Mutex(false, "JESS.Client.Tray.Singleton", out bool mutexNotExists);
                                if (mutexNotExists) {
                                    // If the monitor is not there, start one
                                    StartMonitorInstance(selectedItems);
                                } else {
                                    // Show dialog and update config
                                    AddWatchingFolder(selectedItems);
                                }
                            } catch (AbandonedMutexException) {
                                // not sure what to do with this
                                log.Error("Abandoned Mutex exception.");
                            }
                        }

                    } finally {

                    }
                }

                //2.Handle Command line argument
                else {
                    Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o => {

                        if (o.ExitCode != 99) {
                            Environment.Exit(o.ExitCode); // Mock exit code
                        }

                        String cfg_file = null;
                        if (o.Cfg != null && File.Exists(o.Cfg)) {
                            cfg_file = o.Cfg;
                            GlobalUtility.ConfigFilepath = cfg_file;
                        } else if (File.Exists(GlobalUtility.ConfigFilepath)) {
                            cfg_file = GlobalUtility.ConfigFilepath;
                        }
                        //1.Read Config file to fecth server url and session.
                        GlobalUtility.Config = ClientConfig.ParseClientConfig(cfg_file);
                        if (GlobalUtility.Config != null) {
                            log.Info($"Client config loaded from {cfg_file}. Number of pending jobs = {GlobalUtility.Config.PendingJobs.Count}");
                        } else {
                            log.Info($"Failed to load client config from {cfg_file}. Defaults are used.");
                            GlobalUtility.Config = new ClientConfig();
                        }

                        if (!JESSProcess.InfoTransaction()) {
                            Environment.Exit(-99); // JESS Service not available
                        }
                        if (o.CheckIn) {

                            if (!JESSProcess.CheckInTransaction()) {
                                frmLogin frm = new frmLogin();
                                frm.ShowDialog();
                                if (frm != null) { frm.Dispose(); frm = null; }
                            } else {
                                log.Info("Check-in successful. Session key has been updated.");
                            }

                        }

                        if (o.Submit != null) {

                            string title = "", desc = "";

                            if (o.Desc != null && o.Desc.Count() > 0) {
                                List<string> descriptions = o.Desc.ToList();
                                title = descriptions [0];
                                if (o.Desc.Count() > 1) {
                                    desc = descriptions [1];
                                }
                            }
                            List<string> options = new List<string> { };
                            if (o.Option != null && o.Option.Count() > 0) {
                                options = o.Option.ToList();
                            }

                            var formData = new Dictionary<string, string> {
                                    { "title", title },
                                    { "desc", desc }
                                };

                            if (o.Submit.ToUpper().Equals("JE_JEP")) {
                                //var formData = new Dictionary<string, string>
                                //{
                                //    { "type", "These are my files" },
                                //    { "title", "These are my files" },
                                //    { "desc", "These are my files" },
                                //    { "model", "These are my files" },
                                //    { "subset", "These are my files" },
                                //    { "cases", "misc" }
                                //};

                                formData.Add("type", "JEP");
                                if (options.Count > 0) {
                                    formData.Add("subset", options [0]);
                                }
                                if (options.Count > 1) {
                                    formData.Add("cases", options [1]);
                                }

                            } else {
                                //var formData = new Dictionary<string, string>
                                //    {
                                //    { "type", "These are my files" },
                                //    { "title", "These are my files" },
                                //    { "desc", "These are my files" },
                                //    { "model", "These are my files" },
                                //    { "weather", "These are my files" },
                                //    { "split", "misc" }
                                //};
                                formData.Add("type", "EP");
                                if (o.Submit.ToUpper().Equals("EP_IDF_SPLIT")) {
                                    formData.Add("split", "true");
                                }
                                if (options.Count > 0) {
                                    formData.Add("model", options [0]);
                                }
                                if (options.Count > 1) {
                                    formData.Add("weather", options [1]);
                                }
                            }

                            // Prepare files to upload
                            List<FileInfo> files = new List<FileInfo> { };
                            if (o.Files != null && o.Files.Count() > 0) {
                                string zipfile = "tosubmit.zip";
                                zipfile = GlobalUtility.CreateZipArchive(zipfile, o.Files.ToList());
                                files.Add(new FileInfo(@zipfile));
                            }

                            // Upload and run
                            var result = JESSProcess.SubmitTransaction(files, formData);

                            if (result.Ok) {
                                string target = null;
                                if (!String.IsNullOrEmpty(o.Output)) {
                                    target = o.Output;
                                }
                                // Save pending job record to config file
                                ClientConfig.PendingJobRecord job = new ClientConfig.PendingJobRecord(result.Data, null, target);
                                GlobalUtility.Config.PendingJobs.Add(job.Id.ToString(), job);
                                GlobalUtility.Config.PersistClientConfig(GlobalUtility.ConfigFilepath);

                                log.Info($"Submission successful. Job {job.Id.ToString()} has been added to the pending jobs list.");

                                // Wait for results?
                                //if (o.Await) {
                                //    JESSProcess.StartAutoRetrievalTimer(10, job.Id.ToString());
                                //}
                            }

                        } else if (!String.IsNullOrEmpty(o.Retrieve)) {

                            // Upload and run
                            bool result = JESSProcess.CheckAndRetrieveJobs(o.Retrieve);

                            if (result) {

                                log.Info($"Job results downloaded successfully. There are {GlobalUtility.Config.PendingJobs.Count} jobs in the pending jobs list.");

                                // Retrieval completed. Save pending job record to config file
                                GlobalUtility.Config.PersistClientConfig(GlobalUtility.ConfigFilepath);
                            } else {

                                log.Info($"Job results are not ready. There are {GlobalUtility.Config.PendingJobs.Count} jobs in the pending jobs list.");

                                // Wait for results?
                                //if (o.Await) {
                                //    JESSProcess.StartAutoRetrievalTimer(10, o.Retrieve);
                                //}
                            }

                        } else if (o.Cancel != null && o.Cancel.Count() > 0) {

                            List<string> jobs = o.Cancel.ToList();

                            // Cancel jobs
                            bool result = JESSProcess.CancelJobs(jobs);

                            if (result) {

                                log.Info($"Cancel command executed successfully. There are {GlobalUtility.Config.PendingJobs.Count} jobs in the pending jobs list.");

                                // Remove jobs from the pending jobs list record and save to config file
                                GlobalUtility.Config.PersistClientConfig(GlobalUtility.ConfigFilepath);
                            }

                        } else if (o.Watch) {

                            // Check if folder monitor is already running
                            try {
                                Mutex singleton = new Mutex(false, "JESS.Client.Tray.Singleton", out bool mutexNotExists);
                                if (mutexNotExists) {
                                    // If the monitor is not there, start one
                                    StartMonitorInstance(o.Files);
                                } else {
                                    // Show dialog and update config
                                    AddWatchingFolder(o.Files);
                                }
                            } catch (AbandonedMutexException) {
                                // not sure what to do with this
                                log.Error("Abandoned Mutex exception.");
                            }

                        } else {

                        }
                    })
                    .WithNotParsed(errs => {
                        log.Error("Invalid command-line arguments.");
                    });
                }
            } else {
                Console.WriteLine("JESS_Web Client v1.0.0 (C) 2024 Energy Simulation Solutions Ltd. All rights reserved. ");
            }

        }

        /// <summary>
        /// Start a folder watcher and polling for results
        /// </summary>
        public static void StartMonitorInstance(IEnumerable<string> items) {

            log.Debug($"Starting monitor with items: {String.Join(", ", items)}");

            if (items != null && items.Count() > 0) {
                foreach (string path in items) {
                    if (Directory.Exists(path)) {
                        GlobalUtility.Config.AddFolderMonitor(new FolderMonitor {
                            Path = @path,
                            Type = MonitorType.Subfolders,
                            LastCheckTime = DateTime.Now
                        });
                        log.Info("Folder watcher added for " + path);
                    }
                }
            }
            TrayIconApplicationContext watcher = new TrayIconApplicationContext();
            Application.Run(watcher);
        }

        /// <summary>
        /// Add the given list of items to the watched folders, show folder manager dialog, and save configuration
        /// </summary>
        public static void AddWatchingFolder(IEnumerable<string> items) {

            log.Debug($"Adding items to monitor: {String.Join(", ", items)}");

            if (items != null && items.Count() > 0) {
                foreach (string path in items) {
                    if (Directory.Exists(path)) {
                        GlobalUtility.Config.AddFolderMonitor(new FolderMonitor {
                            Path = @path,
                            Type = MonitorType.Subfolders,
                            LastCheckTime = DateTime.Now
                        });
                        log.Info("Folder watcher added for " + path);
                    }
                }
            }
            Form managerForm = new FolderManager();
            managerForm.Show();

        }

        /// <summary>
        /// If files/folder to be uploaded count greater than 0 then start the upload process.
        /// </summary>
        public static void StartUploadtoJESS(List<string> items, string caption) {

            log.Debug($"Uploading job ({caption}) with files: {String.Join(", ", items)}");

            // First, check in
            if (!JESSProcess.CheckInTransaction()) {
                if (! GlobalUtility.LogonDialogShown) {
                    frmLogin frm = new frmLogin();
                    frm.ShowDialog();
                    if (frm != null) { frm.Dispose(); frm = null; }
                }
            } else {
                log.Info("Check-in successful. Session key has been updated.");
            }

            if (GlobalUtility.LoggedOn) {
                // Check folder size
                if (items.Count == 1 && Directory.Exists(items[0])) {
                    var size = CalcFolderSize(items [0]);
                    if (size > 100) {
                        log.Warn($"Folder {items [0]} ({size}MB) is too large to upload.");
                        return;
                    }
                }
                
                // Start submitting
                var formData = new Dictionary<string, string> {
                                    { "title", caption },
                                    { "desc", String.Join(" ", items) }
                                };
                // Prepare files to upload
                if (items != null && items.Count() > 0) {

                    List<FileInfo> files = new List<FileInfo> { };

                    string zipfile = AppDataStorage.GetAppDataPath() + "\\" + "tosubmit.zip";
                    zipfile = GlobalUtility.CreateZipArchive(zipfile, items);
                    files.Add(new FileInfo(@zipfile));

                    // Upload and run
                    var result = JESSProcess.SubmitTransaction(files, formData);

                    if (result != null && result.Ok) {
                        DialogResult resp = MessageBox.Show(
                            $"Job submitted with id {result.Data}. Go to the web portal?",
                            "Job submitted!",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information);
                        if (resp == DialogResult.Yes) {
                            System.Diagnostics.Process.Start("https://app.ensims.com/jess");
                        }
                        if (File.Exists(zipfile)) {
                            File.Delete(zipfile);
                        }

                        // Save pending job record to config file
                        ClientConfig.PendingJobRecord job = new ClientConfig.PendingJobRecord(result.Data, null, String.Join("-", items) + ".output");
                        GlobalUtility.Config.PendingJobs.Add(job.Id.ToString(), job);
                        GlobalUtility.Config.PersistClientConfig(GlobalUtility.ConfigFilepath);

                        log.Info($"Submission successful. Job {job.Id.ToString()} has been added to the pending jobs list.");
                    } else {
                        MessageBox.Show($"Job submission failed: {result.Status}", "Message", MessageBoxButtons.OK);
                    }
                }
            }else {
                log.Info("Please log on first.");
            }
        }

        private static double CalcFolderSize (String path) {
            // Get the file from the path 
            string [] list = Directory.GetFiles(path);

            // Get the average size 
            double sum = list.Select(file => new FileInfo(file).Length).Sum() / 1000000;

            // Round off the average size to 1 decimal point 
            sum = Math.Round(sum, 1);

            log.Debug($"Size of {path} is {sum} MB");

            return sum; // in MB
        }

    }
}
