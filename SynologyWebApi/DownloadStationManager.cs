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
        /// Use to initialize from an existing list of stored
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
        /// Performs an synchronous login with all sessions.
        /// </summary>
        /// <returns></returns>
        public async Task LoginAsync()
        {
            // Create a query that, when executed, returns a collection of login tasks.
            IEnumerable<Task<bool>> loginTasksQuery =
                from connection in _SessionList select this.LoginAsync(connection.Session);

            // Use ToList to execute the query and start the tasks. 
            List<Task<bool>> loginTasks = loginTasksQuery.ToList();

            // Add a loop to process the tasks one at a time until none remain. 
            while (loginTasks.Count > 0)
            {
                // Identify the first task that completes.
                Task<bool> firstFinishedTask = await Task.WhenAny(loginTasks);

                // Remove the selected task from the list so that you don't 
                // process it more than once.
                loginTasks.Remove(firstFinishedTask);

                // Await the completed task. 
                bool success = await firstFinishedTask;
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
                if(_SessionList.AddSession(session))
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

        private async Task<bool> LoginAsync(DownloadStationApi session)
        {
            ApiVersionInfo info = await session.QueryApiInfoAsync();

            if (info != null)
            {
                return await session.LoginAsync();
            }

            return false;
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
