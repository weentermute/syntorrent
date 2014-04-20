using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynologyWebApi
{
    /// <summary>
    /// Serves as a front-end view for a connection.
    /// </summary>
    public class ConnectionViewModel : INotifyPropertyChanged
    {
        public ConnectionViewModel(DownloadStationApi session)
        {
            _WebSession = session;
        }

        public string ConnectionId
        {
            get
            {
                return _WebSession.LoginInfo;
            }
        }

        public string LastMessage
        {
            get
            {
                return _WebSession.ProgressMessage;
            }
        }

        public DownloadStationApi Session
        {
            get { return _WebSession; }
        }

        public DownloadStationApi WebSession { get { return _WebSession; } }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion // INotifyPropertyChanged Members

        #region Private Members

        private DownloadStationApi _WebSession; 

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    public class ConnectionViewModelList : ObservableCollection<ConnectionViewModel>
    {
        public ConnectionViewModelList()
        {
        }

        public void AddSession(DownloadStationApi session)
        {
            Add(new ConnectionViewModel(session));
        }
    }
}
