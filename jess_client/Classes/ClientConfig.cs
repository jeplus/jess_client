using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using log4net;
using System.Threading;

namespace ensims.jess_client.Classes {

    public class FolderMonitor {
        public static bool MonitorOn { get; set; }
        public bool Enabled { get; set; }
        public string Path { get; set; }
        public MonitorType Type { get; set; }
        public DateTime LastCheckTime { get; set; }
        public string MarkerFileName { get; set; }
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

        public void AddFolderMonitor(FolderMonitor monitor) {
            WatchedFolders.Add(monitor);
        }

        public void RemoveFolderMonitor(FolderMonitor monitor) {
            WatchedFolders.Remove(monitor);
        }

    }


}