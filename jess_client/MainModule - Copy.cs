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
using System.Threading.Tasks;
using Shell32;
using SHDocVw;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

[ComImport]
[Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39")]
public interface IShellWindows
{
    [DispId(0)]
    Shell32.IShellFolderViewDual2 Item(object index);
}

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

        public class JobRunner {
            private static readonly int POLL_INTERVAL_MS = 5000;  // 5 seconds between status checks

            public static int RunAndWaitForCompletion(string jobId) {
                log.Debug("Enter Awaiting result for " + jobId);
                DateTime _lastActionTime = DateTime.UtcNow;
                TimeSpan _actionInterval = TimeSpan.FromHours(8);
                int _exitCode = 0;

                if (string.IsNullOrEmpty(jobId)) {
                    log.Error("No job ID provided for monitoring");
                    return 4;   // Unspecified error
                }

                try {
                    while (true) {
                        // Check if check-in is required
                        if (DateTime.UtcNow - _lastActionTime >= _actionInterval) {
                            CheckIn();
                            _lastActionTime = DateTime.UtcNow;
                        }

                        if (GlobalUtility.LoggedOn) {
                            // Try to retrieve the job
                            bool result = JESSProcess.CheckAndRetrieveJobs(jobId);

                            if (result) {
                                // Job completed successfully
                                log.Info($"Job {jobId} completed and results retrieved successfully");
                                GlobalUtility.Config.PersistClientConfig(GlobalUtility.ConfigFilepath);
                                return 0; // Successful
                            }

                            log.Debug($"{jobId} is not yet finished. Continue...");
                            // Wait before next check
                            Task.Delay(POLL_INTERVAL_MS).Wait();
                            log.Debug($"Waiting for job {jobId} to complete...");
                        }else {
                            log.Info("Check-in failed. Please use -k to log-on again.");
                            return 1; // Unauthorized
                        }
                    }
                } catch (CustomHttpRequestException cre) {
                    if (cre.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
                        log.Error($"Unauthorized error while monitoring job {jobId}: {cre.Message}");
                        _exitCode = 1; // Unauthorized
                    } else {
                        log.Error($"Network/server error while monitoring job {jobId}: {cre.Message}");
                        _exitCode = 2; // Network or server error
                    }
                } catch (IOException ex) {
                    log.Error($"Local error while monitoring job {jobId}: {ex.Message}");
                    _exitCode = 3; // Local (IO) error
                } catch (Exception ex) {
                    log.Error($"Error while monitoring job {jobId}: {ex.Message}");
                    _exitCode = 4; // Unknown error
                }

                log.Info($"Stopped monitoring job {jobId} for unknown reason.");
                return _exitCode;
            }
        }

        public class Options {
            [Option('f', "cfg", Required = false, HelpText = "Specify the configuration file")]
            public string Cfg { get; set; }

            [Option('x', "exitcode", Required = false, Default = 99, HelpText = "Return directly the exit code (for debugging purpose)")]
            public int ExitCode { get; set; }

            [Option('e', "energyplus", SetName = "eplus", Required = false, HelpText = "Run the client in the energyplus.exe mode. Equivalent to '-s EP -o . -a in.idf in.epw'")]
            public bool EPlus { get; set; }

            [Option('k', "checkin", Required = false, HelpText = "Check in to the online service with the exist key")]
            public bool CheckIn { get; set; }

            [Option('s', "submit", SetName = "submit", Required = false, HelpText = "Submit a job of the specified type: JEP, EP, EP-SPLIT, RTRACE, RPICT, DS and AUTO")]
            public string Submit { get; set; }

            [Option('d', "desc", SetName = "submit", Required = false, Separator = ':', HelpText = "Specify the title and the description of the submitted job. The fields are separated by ':'.")]
            public IEnumerable<string> Desc { get; set; }

            [Option('t', "options", Required = false, Separator = ':', HelpText = "Specify the job submission or retrieval options. Multiple fields are separated by ':'")]
            public IEnumerable<string> Option { get; set; }

            [Option('g', "args", SetName = "submit", Required = false, HelpText = "Only used by Radiance jobs for specifying rtrace/rpict program arguments in a file, e.g. with @args_file")]
            public string Args { get; set; }

            [Option('o', "output", Required = false, HelpText = "Specify the job's output folder for storing the results. This can be specified at submission or retrieving time.")]
            public string Output { get; set; }

            [Option('a', "await", Required = false, HelpText = "Works with --submit and --retrieve only. Awaits the results to become available and retrieve")]
            public bool Await { get; set; }

            [Option('r', "retrieve", SetName = "retrieve", Required = false, HelpText = "Retrieve a job (with the id) or all pending jobs (with 'all'). Results are downloaded if simulation has completed, otherwise returns the job status")]
            public string Retrieve { get; set; }

            [Option('w', "watch", SetName = "watch", Required = false, HelpText = "Start folder watching and awaiting results")]
            public bool Watch { get; set; }

            [Option('c', "cancel", SetName = "cancel", Required = false, Separator = ':', HelpText = "Cancel one or more pending jobs (with the id or ids separated by ':') or all pending jobs (with 'all'). Status of the job(s) will be returned")]
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
        static int Main(string [] args) {

            // log.Debug("Commandline args: " + String.Join(", ", args));
            int _exitCode = 0;

            // If application is neither launched frpm context menu or comman line, exit the application.
            if (args.Length > 0) {

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                if (GlobalUtility.SelectedFilesFolder == null) { GlobalUtility.SelectedFilesFolder = new ArrayList(); }


                //1. If application is launched through context menu.
                if (args [0].ToString().ToUpper().StartsWith("CONTEXTMENU")) {

                    GlobalUtility.Config = new ClientConfig();
                    // Load config
                    GlobalUtility.ConfigFilepath = Path.Combine(AppDataStorage.GetAppDataPath(), GlobalUtility.defaultConfigFileName);
                    if (File.Exists(GlobalUtility.ConfigFilepath)) {
                        GlobalUtility.Config = ClientConfig.ParseClientConfig(GlobalUtility.ConfigFilepath);
                    }

                    try {
                        List<string> selectedItems = new List<string>();

                        // Get the active Explorer window handle
                        int handle = GetForegroundWindow();
                        log.Debug($"Active window handle: {handle}");
                        
                        // Get Shell application
                        Shell32.Shell shell = new Shell32.Shell();
                        
                        // Get the shell windows
                        ShellWindows shellWindows = new ShellWindows();
                        
                        // Iterate through windows to find Explorer
                        foreach (InternetExplorer window in shellWindows) {
                            log.Debug($"Found window: HWND={window.HWND}, Name={window.FullName}");
                            try {
                                if (window.HWND == handle) {
                                    // Get the shell window interface
                                    Shell32.IShellFolderViewDual2 shellWindow = window.Document as Shell32.IShellFolderViewDual2;
                                    if (shellWindow != null) {
                                        // Get selected items
                                        FolderItems items = shellWindow.SelectedItems();
                                        foreach (FolderItem item in items) {
                                            selectedItems.Add(item.Path);
                                        }
                                        break;
                                    }
                                }
                            } catch (Exception ex) {
                                log.Debug($"Error processing window: {ex.Message}");
                                continue;
                            }
                        }

                        if (selectedItems.Count == 0) {
                            log.Warn("No items were selected from Explorer");
                            return 1;
                        }

                        log.Debug($"Selected items from Explorer: {String.Join(", ", selectedItems)}");

                        if (args [0].ToString().ToUpper().EndsWith("1")) {
                            SubmitArbitaryJob(selectedItems, "Manual submission");

                        } else if (args [0].ToString().ToUpper().EndsWith("2")) {
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
                                log.Error("Abandoned Mutex exception.");
                            }
                        }

                    } catch (Exception ex) {
                        log.Error($"Error processing context menu command: {ex.Message}");
                        return 1;
                    }
                }

                //2.Handle Command line argument
                else {
                    Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o => {

                        if (o.ExitCode != 99) {
                            switch (o.ExitCode) {
                                case 0:
                                    log.Info("Operation completed successfully.");
                                    break;
                                case 1:
                                    log.Info("Unauthorized operation. Please use --checkin");
                                    break;
                                case 2:
                                    log.Info("Server error. If the error persists, contact support");
                                    break;
                                case 3:
                                    log.Info("Local error occured. Please check the files/folders are accessible.");
                                    break;
                                case 4:
                                    log.Info("Operation failed due to undefined error(s).");
                                    break;
                                case -99:
                                    log.Info("Cannot reach JESS server. Please check if https://app.ensims.com is accessible.");
                                    break;
                                default:
                                    log.Info($"Client exit code = {o.ExitCode}");
                                    break;
                            }
                            Environment.Exit(o.ExitCode); // Mock exit code
                        }

                        if (o.Cfg != null && File.Exists(o.Cfg)) {
                            GlobalUtility.ConfigFilepath = o.Cfg;
                        } else {
                            string cfgpath = Path.Combine(AppDataStorage.GetAppDataPath(), GlobalUtility.defaultConfigFileName);
                            if (IsFileWriteable(cfgpath)) {
                                GlobalUtility.ConfigFilepath = cfgpath;
                            } else {
                                cfgpath = Path.Combine(GlobalUtility.appFilePath, GlobalUtility.defaultConfigFileName);
                                if (IsFileWriteable(cfgpath)) {
                                    GlobalUtility.ConfigFilepath = cfgpath;
                                } else {
                                    if (IsFileWriteable(GlobalUtility.defaultConfigFileName)) {
                                        GlobalUtility.ConfigFilepath = GlobalUtility.defaultConfigFileName;
                                    }
                                }
                            }
                        }

                        //1.Read Config file to fecth server url and session.
                        GlobalUtility.Config = ClientConfig.ParseClientConfig(GlobalUtility.ConfigFilepath);
                        if (GlobalUtility.Config != null) {
                            log.Info($"Client config loaded from {GlobalUtility.ConfigFilepath}. Number of pending jobs = {GlobalUtility.Config.PendingJobs.Count}");
                            GlobalUtility.LoggedOn = !String.IsNullOrWhiteSpace(GlobalUtility.Config.SessionKey);
                        } else {
                            log.Info($"Failed to load client config from {GlobalUtility.ConfigFilepath}. Defaults are used.");
                            GlobalUtility.Config = new ClientConfig();
                        }

                        // 2. Check if the JESS service is accessible
                        if (!JESSProcess.InfoTransaction()) {
                            log.Info("Cannot reach the JESS server. Please check if https://app.ensims.com is accessible using a web browser.");
                            Environment.Exit(-99); // JESS Service not available
                        }

                        // 3. Check-in to update the session key. Show the log in dialogue if required
                        if (o.CheckIn) {
                            CheckIn();
                        }

                        // 4. Operations
                        if (GlobalUtility.LoggedOn) {

                            // energyplus.exe mode
                            if (o.EPlus) {

                                var formData = new Dictionary<string, string> {
                                    { "title", "EnergyPlus job from jess_client_w" },
                                    { "desc", Path.GetFullPath(".") }
                                };
                                formData.Add("type", "EP");

                                // Prepare files to upload
                                List<FileInfo> files = new List<FileInfo> { };
                                files.Add(new FileInfo(@Path.GetFullPath("in.idf")));
                                files.Add(new FileInfo(@Path.GetFullPath("in.epw")));

                                // Upload and run
                                var result = JESSProcess.SubmitTransaction(files, formData);

                                if (result.Ok) {
                                    // Save pending job record to config file
                                    ClientConfig.PendingJobRecord job = new ClientConfig.PendingJobRecord(result.Data, null, Path.GetFullPath("."));
                                    GlobalUtility.Config.PendingJobs.Add(job.Id.ToString(), job);
                                    GlobalUtility.Config.PersistClientConfig(GlobalUtility.ConfigFilepath);

                                    log.Info($"Submission successful. Job {job.Id.ToString()} has been added to the pending jobs list.");

                                    // Start auto-retrieving for the submitted job
                                    var jobRunner = new JobRunner();
                                    _exitCode = JobRunner.RunAndWaitForCompletion(job.Id.ToString());
                                }

                            // Submit a job
                            } else if (o.Submit != null) {

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

                                switch (o.Submit.ToUpper()) {
                                    case "JEPLUS":
                                        //var formData = new Dictionary<string, string> {
                                        //    { "type", "JEP" },
                                        //    { "title", "job title" },
                                        //    { "desc", "some description" },
                                        //    { "model", "project.json" },
                                        //    { "subset", "ALL|LHS|LIST_FILE|DEFAULT" },
                                        //    { "cases", "sample size or list file name" }
                                        //};

                                        formData.Add("type", "JEP");
                                        if (options.Count > 0) {
                                            formData.Add("subset", options [0]);
                                        }
                                        if (options.Count > 1) {
                                            formData.Add("cases", options [1]);
                                        }
                                        break;
                                    case "EP":
                                        //var formData = new Dictionary<string, string> {
                                        //    { "type", "EP" },
                                        //    { "title", "job title" },
                                        //    { "desc", "some description" },
                                        //    { "model", "model.idf" },
                                        //    { "weather", "my_weather_file.epw" },
                                        //    { "split", "true|false" }
                                        //};
                                        formData.Add("type", "EP");
                                        if (options.Count > 0) {
                                            formData.Add("model", options [0]);
                                        }
                                        if (options.Count > 1) {
                                            formData.Add("weather", options [1]);
                                        }
                                        break;
                                    case "EP-SPLIT":
                                        formData.Add("type", "EP");
                                        formData.Add("split", "true");
                                        if (options.Count > 0) {
                                            formData.Add("model", options [0]);
                                        }
                                        if (options.Count > 1) {
                                            formData.Add("weather", options [1]);
                                        }
                                        break;
                                    case "RTRACE":
                                        //var formData = new Dictionary<string, string> {
                                        //    { "type", "RAD" },
                                        //    { "title", "job title" },
                                        //    { "desc", "some description" },
                                        //    { "program", "rtrace" },
                                        //    { "model", "my_model.oct" },
                                        //    { "input", "view_def.vd" },
                                        //    { "output", "output file extension" },
                                        //    { "args", "...radiance args" }
                                        //};
                                        formData.Add("type", "RAD");
                                        formData.Add("program", "rtrace");
                                        if (options.Count > 0) {
                                            formData.Add("model", options [0]);
                                        }
                                        if (options.Count > 1) {
                                            formData.Add("input", options [1]);
                                        }
                                        if (options.Count > 2) {
                                            formData.Add("output", options [2]);
                                        }
                                        if (o.Args != null) {
                                            formData.Add("args", o.Args);
                                        }
                                        break;
                                    case "RPICT":
                                        //var formData = new Dictionary<string, string> {
                                        //    { "type", "RAD" },
                                        //    { "title", "job title" },
                                        //    { "desc", "some description" },
                                        //    { "program", "rpict" },
                                        //    { "model", "my_model.oct" },
                                        //    { "input", "view_def.vd" },
                                        //    { "output", "output file extension" },
                                        //    { "args", "...radiance args" }
                                        //};
                                        formData.Add("type", "RAD");
                                        formData.Add("program", "rpict");
                                        if (options.Count > 0) {
                                            formData.Add("model", options [0]);
                                        }
                                        if (options.Count > 1) {
                                            formData.Add("input", options [1]);
                                        }
                                        if (options.Count > 2) {
                                            formData.Add("output", options [2]);
                                        }
                                        if (o.Args != null) {
                                            formData.Add("args", o.Args);
                                        }
                                        break;
                                    case "DAYSIM":
                                        //var formData = new Dictionary<string, string> {
                                        //    { "type", "DS" },
                                        //    { "title", "job title" },
                                        //    { "desc", "some description" },
                                        //    { "model", "my_header.hea" },
                                        //    { "input", "sensors.in" }
                                        //};
                                        formData.Add("type", "DS");
                                        if (options.Count > 0) {
                                            formData.Add("model", options [0]);
                                        }
                                        if (options.Count > 1) {
                                            formData.Add("input", options [1]);
                                        }
                                        break;
                                    case "AUTO":
                                        formData.Add("type", "Scan");
                                        break;
                                    default:
                                        log.Error($"Unable to set job type {o.Submit.ToUpper()}. Only JEP, EP, EP-SPLIT, RTRACE, RPICT, DS and AUTO are supported.");
                                        break;
                                }

                                try { 
                                    // Prepare files to upload
                                    List<FileInfo> files = new List<FileInfo> { };
                                    if (o.Files != null && o.Files.Count() > 0) {
                                        if (o.Files.Count() == 1 && Path.GetExtension(o.Files.First()).Equals(".zip")) {
                                            files.Add(new FileInfo(@o.Files.First()));
                                        } else {
                                            string zipfile = "tosubmit.zip";
                                            zipfile = GlobalUtility.CreateZipArchive(zipfile, o.Files.ToList());
                                            files.Add(new FileInfo(@zipfile));
                                        }
                                    }

                                    // Upload and run
                                    var result = JESSProcess.SubmitTransaction(files, formData);

                                    if (result.Ok) {
                                        string target = null;
                                        if (!String.IsNullOrEmpty(o.Output)) {
                                            target = Path.GetFullPath(o.Output);
                                        }
                                        // Save pending job record to config file
                                        ClientConfig.PendingJobRecord job = new ClientConfig.PendingJobRecord(result.Data, null, target);
                                        GlobalUtility.Config.PendingJobs.Add(job.Id.ToString(), job);
                                        GlobalUtility.Config.PersistClientConfig(GlobalUtility.ConfigFilepath);

                                        log.Info($"Submission successful. Job {job.Id.ToString()} has been added to the pending jobs list.");

                                        // Wait for results?
                                        if (o.Await) {
                                            // Start auto-retrieving for the submitted job
                                            var jobRunner = new JobRunner();
                                            _exitCode = JobRunner.RunAndWaitForCompletion(job.Id.ToString());
                                        }
                                    }
                                } catch (CustomHttpRequestException cre) {
                                    if (cre.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
                                        log.Error(cre.Message);
                                        _exitCode = 1; // Unauthorized
                                    } else {
                                        log.Error(cre.Message);
                                        _exitCode = 2; // Network or server error
                                    }
                                } catch (IOException ex) {
                                    log.Error(ex.ToString());
                                    _exitCode = 3; // Local (IO) error
                                } catch (Exception ex) {
                                    log.Error(ex.ToString());
                                    _exitCode = 4; // Unknown error
                                } finally {
                                }

                            // Retrieve result of one or more jobs
                            } else if (!String.IsNullOrEmpty(o.Retrieve)) {
                                string opt = null;
                                if (o.Option != null && o.Option.Count() > 0) {
                                    opt = o.Option.First();
                                }
                                string outdir = null;
                                if (!String.IsNullOrWhiteSpace(o.Output)) {
                                    outdir = o.Output;
                                }
                                // Check and retrieve result of jobs
                                bool result = JESSProcess.CheckAndRetrieveJobs(o.Retrieve, opt, outdir);
                                try { 
                                    if (result) {

                                        log.Info($"Job results downloaded successfully. There are {GlobalUtility.Config.PendingJobs.Count} jobs in the pending jobs list.");

                                        // Retrieval completed. Save pending job record to config file
                                        GlobalUtility.Config.PersistClientConfig(GlobalUtility.ConfigFilepath);
                                    } else {

                                        log.Info($"Job results are not ready. There are {GlobalUtility.Config.PendingJobs.Count} jobs in the pending jobs list.");

                                        // Wait for results?
                                        if (o.Await) {
                                            // Start auto-retrieving for the submitted job
                                            var jobRunner = new JobRunner();
                                            _exitCode = JobRunner.RunAndWaitForCompletion(o.Retrieve);
                                        }
                                    }
                                } catch (CustomHttpRequestException cre) {
                                    if (cre.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
                                        log.Error(cre.Message);
                                        _exitCode = 1; // Unauthorized
                                    } else {
                                        log.Error(cre.Message);
                                        _exitCode = 2; // Network or server error
                                    }
                                } catch (IOException ex) {
                                    log.Error(ex.ToString());
                                    _exitCode = 3; // Local (IO) error
                                } catch (Exception ex) {
                                    log.Error(ex.ToString());
                                    _exitCode = 4; // Unknown error
                                } finally {
                                }

                            // Cancel one or more jobs
                            } else if (o.Cancel != null && o.Cancel.Count() > 0) {

                                List<string> jobs = o.Cancel.ToList();

                                // Cancel jobs
                                try {
                                    bool result = JESSProcess.CancelJobs(jobs);
                                    if (result) {
                                        log.Info($"Cancel command executed successfully. There are {GlobalUtility.Config.PendingJobs.Count} jobs in the pending jobs list.");

                                        // Remove jobs from the pending jobs list record and save to config file
                                        GlobalUtility.Config.PersistClientConfig(GlobalUtility.ConfigFilepath);
                                    }
                                } catch (CustomHttpRequestException cre) { 
                                    if (cre.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
                                        log.Error(cre.Message);
                                        _exitCode = 1; // Unauthorized
                                    } else {
                                        log.Error(cre.Message);
                                    _exitCode = 2; // Network or server error
                                    }
                                } catch (Exception ex) {
                                    log.Error(ex.ToString());
                                    _exitCode = 4; // Unknown error
                                } finally {
                                }

                            // Start watching a folder
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

                            }

                        }else {
                            log.Info("Not logged on. Please use -k to check-in/log-on first.");
                            _exitCode = 1; // Unauthorized
                        }
                    })
                    .WithNotParsed(errs => {
                        log.Info($"Unrecognized command-line arguments: {errs}");
                    });
                }
            } else {
                Console.WriteLine("JESS_Web Client v1.0.0 (C) 2024 Energy Simulation Solutions Ltd. All rights reserved. ");
            }
            return _exitCode;
        }

        public static bool IsFileWriteable(string filePath) {
            // Check if file exists
            if (!File.Exists(filePath)) {
                try {
                    // If file doesn't exist, check if we can create it
                    using (File.Create(filePath, 1, FileOptions.DeleteOnClose)) { }
                    return true;
                } catch {
                    return false;
                }
            }

            // If file exists, try to open for writing
            try {
                using (FileStream fs = new FileStream(filePath,
                    FileMode.Open,
                    FileAccess.Write,
                    FileShare.None)) {
                    // Attempt to write a single byte
                    //fs.WriteByte(0);
                }
                return true;
            } catch (UnauthorizedAccessException) {
                return false;
            } catch (IOException) {
                return false;
            }
        }

        private static void CheckIn () {
            if (!JESSProcess.CheckInTransaction()) {
                frmLogin frm = new frmLogin();
                frm.ShowDialog();
                if (frm != null) { frm.Dispose(); frm = null; }
            } else {
                log.Info("Check-in successful. Session key has been updated.");
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
                            Path = Path.GetFullPath(@path),
                            Type = MonitorType.MarkerFile,
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
                            Path = Path.GetFullPath(@path),
                            Type = MonitorType.MarkerFile,
                            LastCheckTime = DateTime.Now
                        });
                        log.Info("Folder watcher added for " + path);
                    }
                }
            }
            //Form managerForm = new FolderManager();
            //managerForm.Show();

        }

        /// <summary>
        /// Submitting arbitary files/folders to JESS. This opt is submission only. Jobs are not added to the pending jobs list.
        /// </summary>
        public static void SubmitArbitaryJob(List<string> items, string caption) {

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
                if (totalSize > 200*1000*1000) {
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
                        DialogResult resp = MessageBox.Show(
                            $"Job submitted with id {result.Data}. Go to the web portal?",
                            "Job submitted!",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information);
                        if (resp == DialogResult.Yes) {
                            System.Diagnostics.Process.Start("https://app.ensims.com/jess");
                        }
                        //if (File.Exists(zipfile)) {
                        //    File.Delete(zipfile);
                        //}

                        log.Info($"Submission successful. Check on https://app.ensims.com/jess for job {result.Data.ToString()}.");
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
