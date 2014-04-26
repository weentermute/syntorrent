using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SynologyWebApi
{
    class SessionResources
    {
        public static class Images
        {
            public static Geometry User
            { get { return Geometry.Parse("M42.123207,35.952998C42.123207,35.952998,59.814095,40.166618,61.498001,57.012999L0,57.012999C-3.5527137E-15,57.012999,5.055995,38.480312,20.221399,36.794204L24.326918,54.381586 27.344988,54.356802 30.777908,45.354411 27.573,39.798999 33.978001,39.798999 30.907883,45.495424 34.821835,54.295404 37.174583,54.276085z M30.32205,0C39.69651,0 47.298,7.5988789 47.298,16.975199 47.298,26.351399 39.69651,33.953 30.32205,33.953 20.946487,33.953 13.347,26.351399 13.347,16.975199 13.347,7.5988789 20.946487,0 30.32205,0z"); } }
        }
    }

    /// <summary>
    /// Serves as a front-end view for a connection.
    /// </summary>
    public class ConnectionViewModel : INotifyPropertyChanged
    {
        public ConnectionViewModel(DownloadStationApi session)
        {
            _WebSession = session;
            _WebSession.PropertyChanged += OnSessionPropertyChanged;
            // Default Image
            _Image = SessionResources.Images.User;
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

        public Geometry Image
        {
            get { return _Image; }
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
        private Geometry _Image;

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
