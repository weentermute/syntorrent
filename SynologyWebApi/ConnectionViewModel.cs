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
            _WebSession.PropertyChanged += OnSessionPropertyChanged;
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

        void OnSessionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e == null)
                return;

            if (e.PropertyName == "ProgressMessage")
                OnPropertyChanged("LastMessage");
        }

        #endregion
    }

    /// <summary>
    /// Represents an observable list of connection sessions.
    /// </summary>
    public class ConnectionViewModelList : ObservableCollection<ConnectionViewModel>
    {
        public ConnectionViewModelList()
        {
        }

        /// <summary>
        /// Finds a session with a given account id, if any.
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public DownloadStationApi FindSession(string accountId)
        {
            ConnectionViewModel connection = this.FirstOrDefault(p => p.Session.UserAccount.Id == accountId);
            if (connection != null)
                return connection.Session;
            return null;
        }

        /// <summary>
        /// Tests whether a session with the same user Id is stored in the collection.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public bool Has(DownloadStationApi session)
        {
            return this.Any(p => p.Session.UserAccount.Id == session.UserAccount.Id);
        }

        public bool AddSession(DownloadStationApi session)
        {
            // Add session only user ID is not present
            if( !this.Any( p => p.Session.UserAccount.Id == session.UserAccount.Id ))
            {
                Add(new ConnectionViewModel(session));
                return true;
            }
            return false;
        }
    }
}
