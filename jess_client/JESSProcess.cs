using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ensims.jess_client.Classes;
using System.Windows.Forms;
using log4net;
using System.Timers;
using System.Threading;

namespace ensims.jess_client {

    public class JESSProcess {

        private static readonly ILog log = LogManager.GetLogger(typeof(JESSProcess));
        private static readonly Mutex mut = new Mutex();

        static System.Timers.Timer timer;


        /// <summary>
        /// Test if the server is available
        /// </summary>
        /// <returns>True if successful</returns>
        public static bool InfoTransaction() {
            Boolean retflag = false;
            try {
                var resp = GlobalUtility.JessClient.Get<BeanVersionInfo>(GlobalUtility.Config.JessBaseUrl() + "/info");
                log.Info($"JESS Web Service on {GlobalUtility.Config.ServerUrl}: {resp.Title}, {resp.Notice}");
                retflag = (resp != null);
            } catch (Exception ex) {
                log.Error(ex.ToString());
            }
            return retflag;
        }

        /// <summary>
        /// Log on transaction. This will return session ID to use it further upload requests.
        /// </summary>
        /// <returns>True if successful</returns>
        public static bool LogOnTransaction(string txtEmail, string txtPassword) {
            Boolean retflag = false;
            try {
                var values = new { email = txtEmail, password = txtPassword };
                var resp = GlobalUtility.JessClient.Post<BeanAuthResponse>(GlobalUtility.Config.AuthBaseUrl() + "/auth", values);
                GlobalUtility.LoggedOn = resp.Ok;
                retflag = resp.Ok;
            } catch (Exception ex) {
                GlobalUtility.LoggedOn = false;
                log.Error("Logon error: " + ex.Message);
                MessageBox.Show(ex.Message, "Message", MessageBoxButtons.OK);
            }
            return retflag;
        }

        /// <summary>
        /// Check-In Transaction. This function decides  session validity.
        /// </summary>
        /// <returns></returns>
        public static bool CheckInTransaction() {
            Boolean retflag = false;
            Uri uriJESSServer = new Uri(GlobalUtility.Config.AuthBaseUrl() + "/checkin", UriKind.Absolute);
            try {
                // GlobalUtility.JessClient.setSessionKey(GlobalUtility.Config.SessionKey);
                var resp = GlobalUtility.JessClient.Post<BeanAuthResponse>(GlobalUtility.Config.AuthBaseUrl() + "/checkin", string.Empty);
                retflag = resp.Ok;
                GlobalUtility.LoggedOn = resp.Ok;
            } catch (Exception ex) {
                GlobalUtility.LoggedOn = false;
                log.Info("Checkin unsuccessful: " + ex.Message);
            } finally {
            }
            return retflag;
        }


        /// <summary>
        /// Submit Transaction. This function submit a job using the upload and run method
        /// </summary>
        /// <returns></returns>
        public static BeanDataResponse<long> SubmitTransaction(IEnumerable<FileInfo> files, Dictionary<string, string> formData) {
            BeanDataResponse<long> resp = null;
            Uri uriJESSServer = new Uri(GlobalUtility.Config.JessBaseUrl() + "/job", UriKind.Absolute);
            try {
                // GlobalUtility.JessClient.setSessionKey(GlobalUtility.Config.SessionKey);
                resp = GlobalUtility.JessClient.UploadFiles<BeanDataResponse<long>>(GlobalUtility.Config.JessBaseUrl() + "/job", files, formData);
            } catch (Exception ex) {
                log.Error("Submission failed: " + ex.Message);
                //MessageBox.Show(ex.Message, "Message", MessageBoxButtons.OK);
            } finally {
            }
            return resp;
        }

        /// <summary>
        /// Job Status Transaction. This function sends a status inquiry of the given job
        /// </summary>
        /// <returns></returns>
        public static BeanJobStatus JobStatusTransaction(string job_id) {
            BeanJobStatus resp = null;
            try {
                var res = GlobalUtility.JessClient.Get<BeanDataResponse<BeanJobStatus>>(GlobalUtility.Config.JessBaseUrl() + "/job/status/" + job_id);
                if (res.Ok) {
                    resp = res.Data;
                }
            } catch (Exception ex) {
                log.Error(ex.ToString());
            } finally {
            }
            return resp;
        }

        /// <summary>
        /// Job Cancel Transaction. This function sends a cancel command of the given job to the server
        /// </summary>
        /// <returns></returns>
        public static bool JobCancelTransaction(string job_id) {
            bool retflag = false;
            try {
                var res = GlobalUtility.JessClient.Post<BeanDataResponse<BeanJobStatus>>(GlobalUtility.Config.JessBaseUrl() + "/job/" + job_id, new BeanJobCancelCmd());
                retflag = res.Ok;
            } catch (Exception ex) {
                log.Error(ex.ToString());
            } finally {
            }
            return retflag;
        }


        /// <summary>
        /// Job retrieval transaction. This function request the results files of a job with the download option. The downloaded zip archive is
        /// unpacked into the given output folder.
        /// </summary>
        /// <returns></returns>
        public static bool JobRetrievalTransaction(string job_id, string opt, string extractPath) {
            Boolean retflag = false;

            var args = String.IsNullOrEmpty(opt) ? "" : "?ext=" + opt;

            var folder = String.IsNullOrEmpty(extractPath) ? "./" : extractPath;
            // Ensure the extract path exists
            if (! Directory.Exists(folder)) {
                Directory.CreateDirectory(folder);
            }else {
                // Empty output folder
                System.IO.DirectoryInfo di = new DirectoryInfo(folder);
                foreach (FileInfo file in di.GetFiles()) {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories()) {
                    dir.Delete(true);
                }
            }

            string tempZip = null;
            try {
                tempZip = GlobalUtility.JessClient.DownloadFile(GlobalUtility.Config.JessBaseUrl() + "/job/file/" + job_id + args, Path.GetTempPath());
                ZipFile.ExtractToDirectory(tempZip, folder);
                retflag = true;
            } catch (Exception ex) {
                log.Error(ex.ToString());
            } finally {
                // Clean up the temporary zip file
                if (File.Exists(tempZip)) {
                    File.Delete(tempZip);
                }
            }
            return retflag;
        }

        /// <summary>
        /// Cancel jobs in the list
        /// </summary>
        /// <returns></returns>
        public static bool CancelJobs(List<string> jobs) {
            Boolean retflag = true;
            foreach (string job_id in jobs) {
                if (JobCancelTransaction(job_id)) {
                    log.Info(job_id + ": Cancel command sent");
                    GlobalUtility.Config.PendingJobs.Remove(job_id);
                } else {
                    log.Error(job_id + ": error occured during cancel command");
                    retflag = false;
                }
            }
            return retflag;
        }

        /// <summary>
        /// Retrieve jobs in the list
        /// </summary>
        /// <returns></returns>
        public static bool CheckAndRetrieveJobs(string job) {
            Boolean retflag = true;
            bool listChanged = false;
            List<string> retrieved = new List<string> ();
            if (job.Equals("all", StringComparison.OrdinalIgnoreCase)) {
                foreach (ClientConfig.PendingJobRecord rec in GlobalUtility.Config.PendingJobs.Values) {
                    if(CheckAndRetrieveJob(rec)) {
                        retrieved.Add(rec.Id.ToString());
                    }else {
                        retflag = false;
                    }
                }
            }
            else {
                ClientConfig.PendingJobRecord rec = null;
                if (GlobalUtility.Config.PendingJobs.ContainsKey(job)) {
                    rec = GlobalUtility.Config.PendingJobs [job];
                }else if (long.TryParse(job, out long job_id)) {
                    rec = new ClientConfig.PendingJobRecord(job_id, null, null);
                }else {
                    log.Error($"Job ID {job} must be a number");
                    return false;
                }
                if (CheckAndRetrieveJob(rec)) {
                    retrieved.Add(rec.Id.ToString());
                } else {
                    retflag = false;
                }
            }
            foreach (string job_id in retrieved) {
                GlobalUtility.Config.PendingJobs.Remove(job_id);
                listChanged = true;
            }
            if (listChanged) GlobalUtility.Config.PersistClientConfig(GlobalUtility.ConfigFilepath);
            return retflag;
        }

        private static bool CheckAndRetrieveJob(ClientConfig.PendingJobRecord rec) {
            Boolean retflag = true;
            string job_id = rec.Id.ToString();
            var status = JobStatusTransaction(job_id);
            if (status != null) {
                if (status.Status.Equals("FINISHED")) {
                    if (JobRetrievalTransaction(job_id, rec.Opt, rec.TargetFolder)) {
                        log.Info(job_id + ": results downloaded to " + (String.IsNullOrEmpty(rec.TargetFolder) ? "the current folder" : rec.TargetFolder));
                    } else {
                        // download error
                        log.Error(job_id + ": error occured when downloading results.");
                        retflag = false;
                    }
                } else {
                    log.Info(job_id + " is " + status.Status + ": " + status.Status_Info);
                    retflag = false;
                }
            } else {
                log.Warn(job_id + ": error occured when inquiring the status of job.");
                retflag = false;
            }
            return retflag;
        }


        /// <summary>
        /// Start a retrieval time with the specified query interval and the job_id. If job_id is "all", retrieve
        /// all pending jobs
        /// </summary>
        /// <param name="interval">Timer interval in seconds</param>
        /// <param name="job_id">Job ID or "all" for all pending jobs</param>
        public static void StartAutoRetrievalTimer(int interval, string job_id) {
            // Create a timer with the given interval (in seconds)
            timer = new System.Timers.Timer(interval * 1000);

            // Hook up the Elapsed event for the timer
            timer.Elapsed += (sender, e) => OnTimedEvent(sender, e, job_id);

            // Set the timer to raise the Elapsed event repeatedly
            timer.AutoReset = true;

            // Start the timer
            timer.Enabled = true;
        }

        /// <summary>
        /// Stops the auto-retrieval timer
        /// </summary>
        public static void StopAutoRetrievalTimer() {
            timer.Stop();
            timer.Dispose();
        }


        private static void OnTimedEvent(object sender, ElapsedEventArgs e, string job_id) {
            if (mut.WaitOne(1)) {
                if (CheckAndRetrieveJobs(job_id)) {
                    // StopAutoRetrievalTimer();
                }
                mut.ReleaseMutex();
            }
        }

    }
}
