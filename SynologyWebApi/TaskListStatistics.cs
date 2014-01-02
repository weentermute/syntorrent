using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynologyWebApi
{
    /// <summary>
    /// Provides accumulative statistics for a list of download tasks.
    /// </summary>
    public class TaskListStatistics : INotifyPropertyChanged
    {
        // Boiler plate code to have properties trigger their updates
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Total download size.
        /// </summary>
        public FileSize Download
        {
            get 
            {
                lock (_ThisLock) { return _Downloaded; } 
            }
        }

        /// <summary>
        /// Total download rate.
        /// </summary>
        public FileSize DownloadRate
        {
            get
            {
                lock (_ThisLock) { return _DownloadRate; }
            }
        }

        /// <summary>
        /// Total upload size.
        /// </summary>
        public FileSize Uploaded
        {
            get
            {
                lock (_ThisLock) { return _Uploaded; }
            }
        }

        /// <summary>
        /// Total upload rate.
        /// </summary>
        public FileSize UploadRate
        {
            get
            {
                lock (_ThisLock) { return _UploadRate; }
            }
        }

        /// <summary>
        /// Computes the accumulative statistics of a list of tasks.
        /// </summary>
        /// <param name="tasks"></param>
        /// <returns></returns>
        public async Task<bool> UpdateAsync( IList<DownloadTask> tasks)
        {
            var t = Task.Factory.StartNew<bool>(() => Accumulate(tasks));
            await t;

            if(t.Result)
            {
                NotifyPropertyChanged();
                return true;
            }
            return false;
        }

        private bool Accumulate(IList<DownloadTask> tasks)
        {
            lock (_ThisLock)
            {
                if (_Running)
                    return false;

                _Running = true;
            }
            FileSize downloaded = new FileSize(0);
            FileSize downloadRate = new FileSize(0, "/s");
            FileSize uploaded = new FileSize(0);
            FileSize uploadRate = new FileSize(0, "/s");

            foreach(DownloadTask task in tasks)
            {
                downloaded.SizeBytes += task.Downloaded.SizeBytes;
                downloadRate.SizeBytes += task.DownloadSpeed.SizeBytes;
                uploaded.SizeBytes += task.Uploaded.SizeBytes;
                uploadRate.SizeBytes += task.UploadSpeed.SizeBytes;
            }
            lock (_ThisLock)
            {
                _Downloaded = downloaded;
                _DownloadRate = downloadRate;
                _Uploaded = uploaded;
                _UploadRate = uploadRate;
                _Running = false;
            }
            return true;
        }

        private FileSize _Downloaded;
        private FileSize _DownloadRate;
        private FileSize _Uploaded;
        private FileSize _UploadRate;
        private Object _ThisLock = new Object();
        private bool _Running = false;

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
    }
}
