using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using log4net;
using System.Threading;

namespace ensims.jess_client.Classes {

    public class FolderMonitor {
        public static bool MonitorOn { get; set; } = true;
        public bool Enabled { get; set; } = true;
        public string Path { get; set; }
        public MonitorType Type { get; set; } = MonitorType.MarkerFile;
        public DateTime LastCheckTime { get; set; }
        public string MarkerFileName { get; set; } = "submit!";
        public HashSet<string> PreviousFiles { get; set; }
        public Dictionary<string, DateTime> PreviousSubfolders { get; set; }
    }

    public enum MonitorType {
        Subfolders,
        MarkerFile
    }

    public class ClientConfig {

        private static readonly ILog log = LogManager.GetLogger(typeof(ClientConfig));
        private static readonly Mutex mut = new Mutex(false, "JESS.Client.Config.Writing");

        public class PendingJobRecord {
            public long Id { get; set; }
            public string Opt { get; set; }
            public string TargetFolder { get; set; }

            public PendingJobRecord(long id, string opt, string target) {
                Id = id;
                Opt = opt;
                TargetFolder = target;
            }
        }

        // Useful ??
        public class JobSettings {
            // EnergyPlus
            //var formData = new Dictionary<string, string> {
            //    { "type", "EP" },
            //    { "title", "job title" },
            //    { "desc", "some description" },
            //    { "model", "model.idf" },
            //    { "weather", "my_weather_file.epw" },
            //    { "split", "true|false" }
            //};

            //JEPlus
            //var formData = new Dictionary<string, string> {
            //    { "type", "JEP" },
            //    { "title", "job title" },
            //    { "desc", "some description" },
            //    { "model", "project.json" },
            //    { "subset", "ALL|LHS|LIST_FILE|DEFAULT" },
            //    { "cases", "sample size or list file name" }
            //};

            // rtrace
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

            // rpict
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

            // daysim
            //var formData = new Dictionary<string, string> {
            //    { "type", "DS" },
            //    { "title", "job title" },
            //    { "desc", "some description" },
            //    { "model", "my_header.hea" },
            //    { "input", "sensors.in" }
            //};

            public string Type { get; set; }
            public string Title { get; set; }
            public string Desc { get; set; }
            public string Model { get; set; }
            public string Weather { get; set; }
            public string Split { get; set; }
            public string Subset { get; set; }
            public string Cases { get; set; }
            public string Program { get; set; }
            public string Input { get; set; }
            public string Output { get; set; }
            public string Args { get; set; }

            public JobSettings() {
            }

            public static JobSettings DefaultRTraceSettings () {
                JobSettings js = new JobSettings {
                    Type = "RAD",
                    Model = "model.oct",
                    Program = "rtrace",
                    Input = "*.in",
                    Output = "ill",
                    Args = "-ab 5 -ad 2048 -aa .2 -ar 512 -as 1024 -h+ -I+ -oov -fa"
                };
                return js;
            }

            public static JobSettings DefaultRPictSettings() {
                JobSettings js = new JobSettings {
                    Type = "RAD",
                    Model = "model.oct",
                    Program = "rpict",
                    Input = "*.vf",
                    Output = "hdr",
                    Args = "-S 1 -o sc%%03d.hdr -pa 1.0 -pj 0.9 -pd 0.0 -pm 0.0 -ps 1  -pt 0.05 -w+ -i- -bv+ -dt 0.050 -dc 0.50 -dj 0.7 -ds 0.150 -dr 3 -dp 512 -dv+ -st 0.150 -ab 4 -ar 256 -ad 1024 -as 512 -aa 0.15 -av 0.0 0.0 0.0 -aw 0 -lw 0.002 -ss 1.0 -lr -10 -u- -x 1600 -y 1600 -t 60"
                };
                return js;
            }

        }

        public string Comment { get; set; } = string.Empty;
        public string SessionKey { get; set; } = string.Empty;
        public string ClientId { get; set; } = "JessWebClient";
        public Dictionary<string, PendingJobRecord> PendingJobs { get; set; } = new Dictionary<string, PendingJobRecord>();
        public List<FolderMonitor> WatchedFolders { get; set; } = new List<FolderMonitor>();
        public string ServerUrl { get; set; } = "https://api.ensims.com";
        public string AuthEndPoint { get; set; } = "/users/api";
        public string JessEndPoint { get; set; } = "/jess_web/api";
        public string AuthBaseUrl() { return ServerUrl + AuthEndPoint; }
        public string JessBaseUrl() { return ServerUrl + JessEndPoint; }

        public static ClientConfig ParseClientConfig(string filePath) {
            try {
                // Read the JSON file
                string jsonContent = File.ReadAllText(filePath);

                // Parse the JSON content
                ClientConfig config = JsonConvert.DeserializeObject<ClientConfig>(jsonContent);

                // You can add additional validation or processing here if needed

                log.Debug($"Configuration loaded from {filePath}");
                return config;
            } catch (FileNotFoundException) {
                log.Error($"The file {filePath} was not found.");
                return null;
            } catch (JsonException ex) {
                log.Error($"Failed to parse JSON: {ex.Message}");
                return null;
            } catch (Exception ex) {
                log.Error($"An error occurred: {ex.Message}");
                return null;
            }
        }

        public bool PersistClientConfig(string filePath) {
            try {
                mut.WaitOne();
                // Update the comment with the current timestamp
                Comment = $"Client configuration saved at {DateTime.Now:yyyy-MM-dd_HH.mm.ss}";

                // Serialize the ClientConfig object to JSON
                string jsonContent = JsonConvert.SerializeObject(this, Formatting.Indented);

                // Write the JSON content to the file
                File.WriteAllText(filePath, jsonContent);

                log.Debug($"Client configuration successfully saved to {filePath}");
                mut.ReleaseMutex();
                return true;
            } catch (Exception ex) {
                log.Error($"Error saving client configuration: {ex.Message}");
            }
            mut.ReleaseMutex();
            return false;
        }

        public int AddFolderMonitor(FolderMonitor monitor) {
            int idx = WatchedFolders.FindIndex(item => item.Path == monitor.Path);
            if (idx >= 0) {
                return idx;
            }

            WatchedFolders.Add(monitor);
            return WatchedFolders.Count - 1;
        }

        public void RemoveFolderMonitor(FolderMonitor monitor) {
            WatchedFolders.Remove(monitor);
        }

    }


}