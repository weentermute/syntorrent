﻿using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Web;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace SynologyWebApi
{
    /// <summary>
    /// Provides information about the version of the web interface of a Synology NAS.
    /// </summary>
    public class ApiVersionInfo
    {
        public class ApiAuth
        {
            public string Path;
            public int MinVersion;
            public int MaxVersion;
        };

        public class ApiDownloadStationTask
        {
            public string Path;
            public int MinVersion;
            public int MaxVersion;
        }

        public ApiAuth Auth = new ApiAuth();
        public ApiDownloadStationTask Task = new ApiDownloadStationTask();

        public ApiVersionInfo(Dictionary<string, dynamic> dict)
        {
            Auth.Path = dict["data"]["SYNO.API.Auth"]["path"];
            Auth.MinVersion = dict["data"]["SYNO.API.Auth"]["minVersion"];
            Auth.MaxVersion = dict["data"]["SYNO.API.Auth"]["maxVersion"];

            Task.Path = dict["data"]["SYNO.DownloadStation.Task"]["path"];
            Task.MinVersion = dict["data"]["SYNO.DownloadStation.Task"]["minVersion"];
            Task.MaxVersion = dict["data"]["SYNO.DownloadStation.Task"]["maxVersion"];
        }
    }

    /// <summary>
    /// Class provides methods to communicate with a Synology disk station's download station module.
    /// 
    /// It uses the official Web API provided by Synology as specified in the document
    /// http://ukdl.synology.com/ftp/other/Synology_Download_Station_Official_API_V3.pdf
    /// </summary>
    public class DownloadStationApi : INotifyPropertyChanged
    {
        /// <summary>
        /// Base HTTP address used for communicating the the download station.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool UseHTTPS { get; set; }

        /// <summary>
        /// Holds the username used for a logging into a a new session.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Holds the password used for a logging into a a new session.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Provides an information string for an ongoing connection.
        /// </summary>
        public string LoginInfo 
        { 
            get
            {
                if(SessionID != "")
                    return Username + "@" + Address;
                return "Not logged in";
            }
        }

        /// <summary>
        /// Holds true if there is an existing connection session stored.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return SessionID != "";
            }
        }

        private static Dictionary<int, string> Errors = new Dictionary<int, string>()
        { 
            { 100, "Unknown error" },
            { 101, "Invalid parameter" },
            { 102, "The requested API does not exist" },
            { 103, "The requested method does not exist" },
            { 104, "The requested version does not support the functionality" },
            { 105, "The logged in session does not have permission" },
            { 106, "Session timeout" },
            { 107, "Session interrupted by duplicate login" },

            { 400, "No such account or incorrect password" },
            { 401, "Account disabled"},
            { 402, "Permission denied" },
            { 403, "2-step verification code required" },
            { 404, "Failed to authenticate 2-step verification code" }
        };

        /// <summary>
        /// Returns an error message given an error code of a DownloadStation API response.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static string GetError(int code)
        {
            if (Errors.ContainsKey(code))
                return Errors[code];
            return "Unknown error code " + code;
        }

        private TaskCollection TaskCollectionValue = new TaskCollection();

        /// <summary>
        /// Holds download task collection retrieved during the last query.
        /// </summary>
        public TaskCollection TaskCollection
        {
            get
            {
                lock (this)
                {
                    return TaskCollectionValue;
                }
            }
        }

        private bool IsIdleValue = false;

        /// <summary>
        /// Holds true if the communication is idle.
        /// </summary>
        public bool IsIdle
        {
            get
            {
                lock (this) { return IsIdleValue; }
            }
            set
            {
                lock (this) { IsIdleValue = value; }
            }
        }

        public delegate void ProgressHandler(object sender, EventArgs e);
        public event ProgressHandler ProgressEvent;

        /// <summary>
        /// Human friendly progress message or result of the last operation.
        /// </summary>
        public string ProgressMessage
        {
            get
            {
                lock (this)
                {
                    return this.ProgressMessageValue;
                }
            }
            set
            {
                lock (this)
                {
                    if (value != this.ProgressMessageValue)
                    {
                        this.ProgressMessageValue = value;
                        NotifyPropertyChanged();
                    }                
                }
                if (this.ProgressEvent != null)
                    this.ProgressEvent(this, new EventArgs());
            }
        }

        /// <summary>
        /// Synology API version info.
        /// </summary>
        public ApiVersionInfo ApiInfo { get; set; }

        /// <summary>
        /// Holds true if we are ready log into a new session.
        /// </summary>
        public bool ReadyToLogin { get { return Username != "" && Address != ""; } }

        private string ProgressMessageValue = String.Empty;

        // Boiler plate code to have properties trigger their updates
        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessors of each property. 
        // The CallerMemberName attribute that is applied to the optional propertyName 
        // parameter causes the property name of the caller to be substituted as an argument. 
        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Notify if ready to login.
        /// </summary>
        /// 
        public void VerifyNotify()
        {
            // Refresh data-binding
            NotifyPropertyChanged();
        }

        public DownloadStationApi()
        {
            Address = "";
            UseHTTPS = false;
            Username = "";
            Password = "";
            ProgressMessageValue = "";
        }

        private string URL
        {
            get
            {
                string url = "";
                if (UseHTTPS)
                {
                    url = "https://" + Address + ":5001";
                }
                else
                {
                    url = "http://" + Address + ":5000";
                }
                url += "/webapi/";
                return url;
            }
        }

        private bool HasCredentials()
        {
            return ((Username != "") && (Password != ""));
        }

        private string SessionIDValue;

        /// <summary>
        /// Session ID of ongoing session.
        /// </summary>
        public string SessionID
        {
            get
            {
                lock (this)
                {
                    return SessionIDValue;
                }
            }
            set
            {
                lock (this)
                {
                    SessionIDValue = value;
                }
            }
        }

        /// <summary>
        /// Delegate type called when query finished.
        /// </summary>
        /// <param name="sender"></param>
        public delegate void QueryApiInfoHandler(object sender, QueryApiInfoEventArgs e);
        public event QueryApiInfoHandler QueryApiInfoEvent;

        /// <summary>
        /// Queries the API version info.
        /// </summary>
        /// <returns></returns>
        public ApiVersionInfo QueryApiInfo()
        {
            // /webapi/query.cgi?api=SYNO.API.Info&version=1&method=query&query=SYNO.API.Auth,SYNO.DownloadStation.Task
            IsIdle = false;
            try
            {
                ProgressMessage = "Querying " + URL;

                HttpRequest syno = new HttpRequest();
                syno.GetParameters.Add("api", "SYNO.API.Info");
                syno.GetParameters.Add("version", "1");
                syno.GetParameters.Add("method", "query");
                syno.GetParameters.Add("query", "SYNO.API.Auth,SYNO.DownloadStation.Task");

                string jsonResponse = syno.Get(URL + "query.cgi");
                var jss = new JavaScriptSerializer();
                var dict = jss.Deserialize<Dictionary<string, dynamic>>(jsonResponse);

                if (dict["success"] == true)
                {
                    ApiInfo = new ApiVersionInfo(dict);

                    // Notify
                    if (QueryApiInfoEvent != null)
                        QueryApiInfoEvent(this, new QueryApiInfoEventArgs(ApiInfo));

                    IsIdle = true;

                    return ApiInfo;
                }
            }
            catch (WebException ex)
            {
                ProgressMessage = ex.Message;
            }
            catch (UriFormatException ex)
            {
                ProgressMessage = ex.Message;
            }

            // Notify
            if (QueryApiInfoEvent != null)
                QueryApiInfoEvent(this, null);
            IsIdle = true;

            // Reset Session Id if no connection was possible
            SessionID = "";

            return null;
        }

        public delegate void LoginHandler(object sender, ApiRequestResultEventArgs e);
        public event LoginHandler LoginEvent;

        /// <summary>
        /// Performs a login and creates a new user session.
        /// </summary>
        /// <returns></returns>
        public bool Login()
        {
            IsIdle = false;
            // GET /webapi/auth.cgi?api=SYNO.API.Auth&version=2&method=login&account=admin&passwd=12345&session=DownloadStation&format=sid
            if(SessionID != null && SessionID != "")
            {
                if (LoginEvent != null)
                    LoginEvent(this, new ApiRequestResultEventArgs(true));
                IsIdle = true;
                return true;
            }
            try
            {
                Logout();

                ProgressMessage = "Logging in...";

                HttpRequest syno = new HttpRequest();
                syno.GetParameters.Add("api", "SYNO.API.Auth");
                syno.GetParameters.Add("version", "2");
                syno.GetParameters.Add("method", "login");
                syno.GetParameters.Add("account", Username);
                syno.GetParameters.Add("passwd", Password);
                syno.GetParameters.Add("session", "DownloadStation");
                syno.GetParameters.Add("format", "sid");

                string jsonResponse = syno.Get(URL + "auth.cgi");
                var jss = new JavaScriptSerializer();
                var dict = jss.Deserialize<Dictionary<string, dynamic>>(jsonResponse);

                if (dict["success"] == true)
                {
                    SessionID = dict["data"]["sid"];

                    ProgressMessage = "Logged in successfully";
                    if (LoginEvent != null)
                        LoginEvent(this, new ApiRequestResultEventArgs(true));
                    IsIdle = true;
                    return true;
                }
                else
                {
                    SessionID = "";

                    ProgressMessage = "Failed to login: " + GetError(dict["error"]["code"]);

                    if (LoginEvent != null)
                        LoginEvent(this, new ApiRequestResultEventArgs(false));
                    IsIdle = true;
                    return false;
                }
            }
            catch (WebException ex)
            {
                ProgressMessage = ex.Message;
            }
            catch (KeyNotFoundException ex)
            {
                ProgressMessage = "Invalid response: " + ex.Message;
            }
            if (LoginEvent != null)
                LoginEvent(this, new ApiRequestResultEventArgs(false));
            IsIdle = true;
            return false;
        }

        public delegate void GetTaskListHandler(object sender, ApiRequestResultEventArgs e);
        public event GetTaskListHandler GetTaskListEvent;

        /// <summary>
        /// Retrieves a list of download tasks from the download station.
        /// 
        /// </summary>
        /// <returns>Collection of tasks.</returns>
        public TaskCollection GetTaskList()
        {
            IsIdle = false;
            // GET /webapi/DownloadStation/task.cgi?api=SYNO.DownloadStation.Task&version=1&method=list
            try
            {
                HttpRequest syno = new HttpRequest();
                syno.GetParameters.Add("_sid", SessionID);
                syno.GetParameters.Add("api", "SYNO.DownloadStation.Task");
                syno.GetParameters.Add("version", ApiInfo.Task.MaxVersion.ToString());
                syno.GetParameters.Add("method", "list");
                syno.GetParameters.Add("additional", "detail,transfer");

                string jsonResponse = syno.Get(URL + ApiInfo.Task.Path);
                var jss = new JavaScriptSerializer();
                var dict = jss.Deserialize<Dictionary<string, dynamic>>(jsonResponse);

                if (dict["success"] == true)
                {
                    TaskCollection collection = new TaskCollection();

                    dynamic tasks = dict["data"]["tasks"];

                    foreach (dynamic task in tasks)
                    {
                        collection.Add(new DownloadTask(task));
                    }

                    TaskCollectionValue = collection;

                    if (GetTaskListEvent != null)
                        GetTaskListEvent(this, new ApiRequestResultEventArgs(true));
                    IsIdle = true;
                    return collection;
                }
                else
                {
                    SessionID = "";

                    ProgressMessage = "Failed to get task list, error: " + dict["error"]["code"];

                    if (GetTaskListEvent != null)
                        GetTaskListEvent(this, new ApiRequestResultEventArgs(false));
                    IsIdle = true;
                    return null;
                }
            }
            catch (WebException ex)
            {
                ProgressMessage = ex.Message;
            }
            catch (Exception ex)
            {
                ProgressMessage = ex.Message;
            }
            if (GetTaskListEvent != null)
                GetTaskListEvent(this, new ApiRequestResultEventArgs(false));
            IsIdle = true;
            return null;
        }

        /// <summary>
        /// Logs out, closes active session.
        /// </summary>
        public void Logout()
        {
            if (SessionID != null && SessionID != "")
            {
                ProgressMessage = "Logging out...";

                // GET /webapi/auth.cgi?api=SYNO.API.Auth&version=1&method=logout&session=DownloadStation
                HttpRequest syno = new HttpRequest();
                syno.GetParameters.Add("_sid", SessionID);
                syno.GetParameters.Add("api", "SYNO.API.Auth");
                syno.GetParameters.Add("version", "1");
                syno.GetParameters.Add("method", "logout");
                syno.GetParameters.Add("session", "DownloadStation");

                string jsonResponse = syno.Get(URL + "auth.cgi");

                var jss = new JavaScriptSerializer();
                var dict = jss.Deserialize<Dictionary<string, dynamic>>(jsonResponse);

                if (dict["success"] == true)
                {
                    ProgressMessage = "Not logged in";
                    SessionID = "";
                }
            }
        }

        public delegate void CreateDownloadTaskHandler(object sender, ApiRequestResultEventArgs e);
        public event CreateDownloadTaskHandler CreateDownloadTaskEvent;

        /// <summary>
        /// Creates a new download task given an URL.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public bool CreateDownloadTask(string uri)
        {
            // POST /webapi/DownloadStation/task.cgi
            // api=SYNO.DownloadStation.Task&version=1&method=create&uri=ftps://192.0.0.1:21/test/test.zip&username=admin&password=123

            ProgressMessage = "Creating download task...";
            IsIdle = false;
            bool success = false;
            try
            {
                HttpRequest syno = new HttpRequest();
                syno.GetParameters.Add("_sid", SessionID);
                syno.GetParameters.Add("api", "SYNO.DownloadStation.Task");
                syno.GetParameters.Add("version", "1");
                syno.GetParameters.Add("method", "create");
                syno.GetParameters.Add("uri", uri);

                string jsonResponse = syno.Get(URL + ApiInfo.Task.Path);
                var jss = new JavaScriptSerializer();
                var dict = jss.Deserialize<Dictionary<string, dynamic>>(jsonResponse);

                success = (dict["success"]);

                if(!success)
                    ProgressMessage = "Failed to create download";
            }
            catch (WebException ex)
            {
                ProgressMessage = ex.Message;
            }

            if(success)
                ProgressMessage = "Task created";

            if (CreateDownloadTaskEvent != null)
                CreateDownloadTaskEvent(this, new ApiRequestResultEventArgs(success));
            IsIdle = true;
            return success;
        }

        /// <summary>
        /// Creates a download task given a local file, typically a .torrent file to be sent
        /// to the Download Station.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public bool CreateDownloadTaskFromFile(string file)
        {
            ProgressMessage = "Creating download task...";
            IsIdle = false;
            bool success = false;
            try
            {
                byte[] fileData = ConvertFileToByteArray(file);

                HttpRequest syno = new HttpRequest();
                syno.PostParameters.Add("_sid", SessionID);
                syno.PostParameters.Add("api", "SYNO.DownloadStation.Task");
                syno.PostParameters.Add("version", "1");
                syno.PostParameters.Add("method", "create");
                syno.PostParameters.Add("file", new FormUpload.FileParameter(fileData, Path.GetFileName(file)));

                string jsonResponse = syno.PostMultipartFormData(URL + ApiInfo.Task.Path);
                var jss = new JavaScriptSerializer();
                var dict = jss.Deserialize<Dictionary<string, dynamic>>(jsonResponse);

                success = (dict["success"]);

                if (!success)
                    ProgressMessage = "Failed to create download";
            }
            catch (WebException ex)
            {
                ProgressMessage = ex.Message;
            }
            catch (Exception ex)
            {
                ProgressMessage = ex.Message;
            }

            if (success)
                ProgressMessage = "Task created";

            if (CreateDownloadTaskEvent != null)
                CreateDownloadTaskEvent(this, new ApiRequestResultEventArgs(success));
            IsIdle = true;
            return success;
        }

        /// <summary>
        /// Pauses a given list of download tasks.
        /// </summary>
        /// <param name="tasks"></param>
        /// <returns></returns>
        public bool PauseDownloadTasks(IList<DownloadTask> tasks)
        {
            bool success = false;
            try
            {
                // Create id list string
                string ids = "";
                foreach(var task in tasks)
                {
                    if (ids != "")
                        ids += ",";
                    ids += task.Id;
                }

                HttpRequest syno = new HttpRequest();
                syno.GetParameters.Add("_sid", SessionID);
                syno.GetParameters.Add("api", "SYNO.DownloadStation.Task");
                syno.GetParameters.Add("version", "1");
                syno.GetParameters.Add("method", "pause");
                syno.GetParameters.Add("id", ids);

                string jsonResponse = syno.Get(URL + ApiInfo.Task.Path);
                var jss = new JavaScriptSerializer();
                var dict = jss.Deserialize<Dictionary<string, dynamic>>(jsonResponse);

                success = (dict["success"]);

                if (!success)
                    ProgressMessage = "Failed to pause downloads";
            }
            catch (WebException ex)
            {
                ProgressMessage = ex.Message;
            }
            catch (Exception ex)
            {
                ProgressMessage = ex.Message;
            }

            return success;
        }

        /// <summary>
        /// Pauses a given list of download tasks.
        /// </summary>
        /// <param name="tasks"></param>
        /// <returns></returns>
        public bool ResumeDownloadTasks(IList<DownloadTask> tasks)
        {
            bool success = false;
            try
            {
                // Create id list string
                string ids = "";
                foreach (var task in tasks)
                {
                    if (ids != "")
                        ids += ",";
                    ids += task.Id;
                }

                HttpRequest syno = new HttpRequest();
                syno.GetParameters.Add("_sid", SessionID);
                syno.GetParameters.Add("api", "SYNO.DownloadStation.Task");
                syno.GetParameters.Add("version", "1");
                syno.GetParameters.Add("method", "resume");
                syno.GetParameters.Add("id", ids);

                string jsonResponse = syno.Get(URL + ApiInfo.Task.Path);
                var jss = new JavaScriptSerializer();
                var dict = jss.Deserialize<Dictionary<string, dynamic>>(jsonResponse);

                success = (dict["success"]);

                if (!success)
                    ProgressMessage = "Failed to resume downloads";
            }
            catch (WebException ex)
            {
                ProgressMessage = ex.Message;
            }
            catch (Exception ex)
            {
                ProgressMessage = ex.Message;
            }

            return success;
        }

        /// <summary>
        /// Deletes a given list of download tasks.
        /// </summary>
        /// <param name="tasks"></param>
        /// <returns></returns>
        public bool DeleteDownloadTasks(IList<DownloadTask> tasks)
        {
            bool success = false;
            try
            {
                // Create id list string
                string ids = "";
                foreach (var task in tasks)
                {
                    if (ids != "")
                        ids += ",";
                    ids += task.Id;
                }

                HttpRequest syno = new HttpRequest();
                syno.GetParameters.Add("_sid", SessionID);
                syno.GetParameters.Add("api", "SYNO.DownloadStation.Task");
                syno.GetParameters.Add("version", "1");
                syno.GetParameters.Add("method", "delete");
                syno.GetParameters.Add("id", ids);

                string jsonResponse = syno.Get(URL + ApiInfo.Task.Path);
                var jss = new JavaScriptSerializer();
                var dict = jss.Deserialize<Dictionary<string, dynamic>>(jsonResponse);

                success = (dict["success"]);

                if (!success)
                    ProgressMessage = "Failed to delete downloads";
            }
            catch (WebException ex)
            {
                ProgressMessage = ex.Message;
            }
            catch (Exception ex)
            {
                ProgressMessage = ex.Message;
            }

            return success;
        }

        /// <summary>
        /// Converts the contents of file into byte buffer.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static byte[] ConvertFileToByteArray(string fileName)
        {
            byte[] returnValue = null;

            using (FileStream fr = new FileStream(fileName, FileMode.Open))
            {
                using (BinaryReader br = new BinaryReader(fr))
                {
                    returnValue = br.ReadBytes((int)fr.Length);
                }
            }
            return returnValue;
        }
    }

    /// <summary>
    /// Carries API version info.
    /// </summary>
    public class QueryApiInfoEventArgs : EventArgs
    {
        public QueryApiInfoEventArgs(ApiVersionInfo e)
        { VersionInfo = e; }

        public ApiVersionInfo VersionInfo { get; set; }
    }

    /// <summary>
    /// Carries the outcome of a HTTP request to Synology API.
    /// </summary>
    public class ApiRequestResultEventArgs : EventArgs
    {
        public ApiRequestResultEventArgs(bool success)
        { Success = success; }

        public bool Success { get; set; }
    }

}
