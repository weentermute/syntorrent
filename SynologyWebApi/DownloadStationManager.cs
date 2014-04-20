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
    /// Manages accounts and sessions with Synology Download Stations.
    /// </summary>
    public class DownloadStationManager : INotifyPropertyChanged
    {
        public DownloadStationManager()
        {

        }

        /// <summary>
        /// Use to to initialize from an existing list of stored
        /// accounts.
        /// </summary>
        /// <param name="accounts"></param>
        public void Initialize(AccountList accounts)
        {
            foreach(var account in accounts)
            {
                AddAccount(account);
            }
        }

        /// <summary>
        /// Adds an account and initializes its session.
        /// </summary>
        /// <param name="account"></param>
        public DownloadStationApi AddAccount(Account account)
        {
            string id = account.Id;

            if(id != "")
            {
                var session = new DownloadStationApi(account);
                _SessionList.AddSession(session);
                return session;
            }
            return null;
        }

        public List<Account> Accounts
        {
            get
            {
                return 
                    (from item in _SessionList 
                     select item.Session.UserAccount).ToList<Account>();
            }
        }

        // Boiler plate code to have properties trigger their updates
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Session view list.
        /// </summary>
        public ConnectionViewModelList Sessions
        {
            get
            {
                return _SessionList;
            }
        }

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

        private ConnectionViewModelList _SessionList = new ConnectionViewModelList();
    }
}
