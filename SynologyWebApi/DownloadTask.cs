using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace SynologyWebApi
{
    /// <summary>
    /// Represents a single download task.
    /// </summary>
    public class DownloadTask : IEquatable<DownloadTask>
    {
        /// <summary>
        /// Converts from Unix epoch time to DateTime.
        /// </summary>
        /// <param name="unixTime"></param>
        /// <returns></returns>
        public static DateTime FromUnixTime(long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }

        /// <summary>
        /// Constructs a DownloadTask from the data dictionary return as a web response when querying tasks.
        /// </summary>
        /// <param name="data"></param>
        public DownloadTask(Dictionary<string, dynamic> data)
        {
            Data = data;
            _Id = GetValue("id", "");
            _File = GetValue("title");
            _Size = new FileSize(GetValue("size", "0"));
            _Status = GetValue("status");
            _UserName = GetValue("username");
            try
            {
                _Destination = Data["additional"]["detail"]["destination"];
            }
            catch (KeyNotFoundException)
            {
                _Destination = "";
            }

            try
            {
                _CreateTime = FromUnixTime(Int64.Parse(Data["additional"]["detail"]["create_time"]));
            }
            catch (KeyNotFoundException)
            {
                _CreateTime = new DateTime(0);
            }

            try
            {
                _Uploaded = new FileSize(Data["additional"]["transfer"]["size_uploaded"]);
            }
            catch (KeyNotFoundException)
            {
                _Uploaded = new FileSize(0);
            }

            try
            {
                _Downloaded = new FileSize(Data["additional"]["transfer"]["size_downloaded"]);
            }
            catch (KeyNotFoundException)
            {
            }

            try
            {
                _UploadSpeed = new FileSize(Data["additional"]["transfer"]["speed_upload"], "/s");
            }
            catch (KeyNotFoundException)
            {
            }

            try
            {
                _DownloadSpeed = new FileSize(Data["additional"]["transfer"]["speed_download"], "/s");
            }
            catch (KeyNotFoundException)
            {
            }

            _TaskStateColor = GetStateColor(Status);

            _DataString = Stringify("", Data);
        }

        private string _Id;

        /// <summary>
        /// The internal ID of a download task.
        /// </summary>
        public string Id
        {
            get { return _Id; }
        }

        private string _File;

        /// <summary>
        /// Main file name of the download.
        /// </summary>
        public string File
        {
            get { return _File; }
        }

        private FileSize _Size;

        /// <summary>
        /// File size of the download task.
        /// </summary>
        public FileSize Size
        {
            get { return _Size; }
        }

        private string _Status;

        /// <summary>
        /// Status string of the download task.
        /// </summary>
        public string Status
        {
            get { return _Status; }
        }

        private string _Destination;

        /// <summary>
        /// Destination folder of the download task.
        /// </summary>
        public string Destination
        {
            get { return _Destination; }
        }

        private DateTime _CreateTime;

        /// <summary>
        /// Creation time of the download task.
        /// </summary>
        public DateTime CreateTime
        {
            get { return _CreateTime; }
        }

        private FileSize _Uploaded;

        public FileSize Uploaded
        {
            get { return _Uploaded; }
        }

        private FileSize _Downloaded;

        public FileSize Downloaded
        {
            get { return _Downloaded; }
        }

        /// <summary>
        /// Download progress in percent.
        /// </summary>
        public double Progress
        {
            get 
            {
                if(_Size.SizeBytes > 0)
                {
                    return (_Downloaded.SizeBytes * 100.0 / _Size.SizeBytes);
                }
                return 0.0;
            }
        }

        /// <summary>
        /// Ratio between uploaded bytes and downloaded bytes.
        /// </summary>
        public double Ratio
        {
            get
            {
                if (_Downloaded.SizeBytes > 0)
                    return (double)_Uploaded.SizeBytes / _Downloaded.SizeBytes;
                return 0.0;
            }
        }

        private FileSize _UploadSpeed;

        /// <summary>
        /// Current upload speed.
        /// </summary>
        public FileSize UploadSpeed
        {
            get { return _UploadSpeed; }
        }

        private FileSize _DownloadSpeed;

        /// <summary>
        /// Current download speed.
        /// </summary>
        public FileSize DownloadSpeed
        {
            get { return _DownloadSpeed; }
        }

        private Color _TaskStateColor;

        /// <summary>
        /// Color of the task's current state.
        /// </summary>
        public Color TaskStateColor
        {
            get { return _TaskStateColor; }
        }


        private string _UserName;

        /// <summary>
        /// The user name of the task's owner.
        /// </summary>
        public string UserName
        {
            get { return _UserName; }
        }

        /// <summary>
        /// Helper function which maps a task status to a color.
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static Color GetStateColor(string status)
        {
            if (status == "error")
                return Colors.LightPink;
            else if (status == "seeding")
                return Colors.LightGreen;
            else if (status == "finished")
                return Colors.Gray;
            else if (status == "paused")
                return Colors.LightSkyBlue;
            return Colors.WhiteSmoke;
        }

        private Dictionary<string, dynamic> Data;
        private string GetValue(string key, string defaultValue = "na")
        {
            dynamic val = "";
            if (Data.TryGetValue(key, out val))
                return val;
            return defaultValue;
        }

        static private string Stringify(string path, object obj)
        {
            if (obj is string)
                return path + (string)obj;

            if (obj is Dictionary<string, object>)
            {
                Dictionary<string, object> objs = (Dictionary<string, object>)obj;
                StringBuilder s = new StringBuilder();
                foreach (var o in objs)
                {
                    s.Append(Stringify(path + o.Key, o.Value));
                }

                return s.ToString();
            }
            return "";
        }

        private string _DataString = "";

        public bool Equals(DownloadTask other)
        {
            return this._DataString == other._DataString;
        }
    }
}
