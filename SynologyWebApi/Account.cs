using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynologyWebApi
{
    /// <summary>
    /// Holds the information of an user account used for a session.
    /// </summary>
    [Serializable()]
    public class Account
    {
        public Account()
        {
            TrustedConnection = false;
            UseHTTPS = false;
        }
        /// <summary>
        /// Base HTTP address used for communicating the the download station.
        /// </summary>
        public string Address = "";

        /// <summary>
        /// If the DS doesn't have a valid certificate this option
        /// can be used to skip the validation.
        /// </summary>
        public bool TrustedConnection;

        /// <summary>
        /// Holds the username used for a logging into a a new session.
        /// </summary>
        public string Username = "";

        /// <summary>
        /// Holds the session ID of the last successful connection.
        /// </summary>
        public string SessionId = "";

        /// <summary>
        /// If set to true the connection will use https://.
        /// </summary>
        public bool UseHTTPS;

        /// <summary>
        /// Holds the password used for a logging into a a new session.
        /// 
        /// Note: this information is not stored into the settings.
        /// </summary>
        [NonSerialized()]
        public string Password = "";

        /// <summary>
        /// Provides a unique identifier for the account.
        /// </summary>
        public string Id
        {
            get
            {
                if(Username != "" && Address != "")
                    return (Username + "@" + Address).ToLower();
                return "";
            }
        }
    }

    /// <summary>
    /// Helper class which manages a list of accounts.
    /// It is used to store the accounts into the settings file.
    /// </summary>
    [Serializable]
    public class AccountList : List<Account>
    {
        public AccountList()
        {
        }
    }
}
