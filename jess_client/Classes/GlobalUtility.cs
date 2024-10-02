using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.IO;
using System.IO.Compression;

using Newtonsoft.Json;
using log4net;

namespace ensims.jess_client.Classes
{
    public static class GlobalUtility {

        private static readonly ILog log = LogManager.GetLogger(typeof(GlobalUtility));

        static ClientConfig cfgConfig = null;
        static string strConfigFilepath = "client.cfg";
        static ArrayList strSelectedFilesFolder = null;
        static bool boolLoggedOn = false;
        static bool boolLogonDialogShown = false;
        static JessHttpClient _JessClient = new JessHttpClient();


        static string strServerSession = string.Empty;
        static string strJESSServer = string.Empty;
        static string strUploadzipFile = string.Empty;
        static string strUploadedzipFileHandle = string.Empty;
        static string strApplicationpath = string.Empty;
        static string strTempPathForZip = string.Empty;

        public static string ConfigFilepath {
            get { return strConfigFilepath; }
            set { strConfigFilepath = value; }
        }

        public static ClientConfig Config {
            get { return cfgConfig; }
            set { cfgConfig = value; }
        }

        public static ArrayList SelectedFilesFolder {
            get { return strSelectedFilesFolder; }
            set { strSelectedFilesFolder = value; }
        }

        public static bool LogonDialogShown {
            get { return boolLogonDialogShown; }
            set { boolLogonDialogShown = value; }
        }

        public static bool LoggedOn {
            get { return boolLoggedOn; }
            set { boolLoggedOn = value; }
        }

        public static JessHttpClient JessClient {
            get { return _JessClient; }
        }



        public static string ServerSession {
            get { return strServerSession; }
            set { strServerSession = value; }
        }

        public static string JESSServer {
            get { return strJESSServer; }
            set { strJESSServer = value; }
        }

        public static string UploadzipFile {
            get { return strUploadzipFile; }
            set { strUploadzipFile = value; }
        }
        public static string UploadedzipFileHandle {
            get { return strUploadedzipFileHandle; }
            set { strUploadedzipFileHandle = value; }
        }



        public static string GetJsonValue(string json, string parameter)
        {
            string retValue = string.Empty;
            try 
            { 
                Dictionary<string, string> LogOnAttributes = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (LogOnAttributes[parameter] != null)  { retValue = LogOnAttributes[parameter]; }                
            }                
            finally { }
            return retValue; 

        }

        public static string Applicationpath
        {
            get { return strApplicationpath; }
            set { strApplicationpath = value; }
        }
        public static string TempPathForZip
        {
            get { return strTempPathForZip; }
            set { strTempPathForZip = value; }
        }

        public static string CreateZipArchive(string outputPath, List<string> itemsToArchive) {
            if (itemsToArchive == null || !itemsToArchive.Any()) {
                throw new ArgumentException("The list of items to archive is empty or null.");
            }

            // Convert the output path to an absolute path
            string absoluteOutputPath = Path.GetFullPath(outputPath);

            using (FileStream zipToCreate = new FileStream(absoluteOutputPath, FileMode.Create)) {
                using (ZipArchive archive = new ZipArchive(zipToCreate, ZipArchiveMode.Create)) {
                    if (itemsToArchive.Count == 1 && Directory.Exists(itemsToArchive [0])) {
                        // If only one folder is given, add its contents directly
                        string folderPath = itemsToArchive [0];
                        string [] files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
                        foreach (string file in files) {
                            string relativePath = file.Substring(folderPath.Length + 1);
                            AddFileToArchive(archive, file, relativePath);
                        }
                    } else {
                        // Add multiple files and folders
                        foreach (string item in itemsToArchive) {
                            if (File.Exists(item)) {
                                AddFileToArchive(archive, item, Path.GetFileName(item));
                            } else if (Directory.Exists(item)) {
                                string [] files = Directory.GetFiles(item, "*", SearchOption.AllDirectories);
                                foreach (string file in files) {
                                    string relativePath = file.Substring(item.Length + 1);
                                    AddFileToArchive(archive, file, Path.Combine(Path.GetFileName(item), relativePath));
                                }
                            } else {
                                log.Warn($"Item not found: {item}");
                            }
                        }
                    }
                }
            }

            return absoluteOutputPath;
        }

        private static void AddFileToArchive(ZipArchive archive, string sourcePath, string entryName) {
            ZipArchiveEntry entry = archive.CreateEntry(entryName);
            using (Stream entryStream = entry.Open())
            using (FileStream fileStream = File.OpenRead(sourcePath)) {
                fileStream.CopyTo(entryStream);
            }
        }


    }
}
