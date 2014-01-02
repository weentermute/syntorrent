using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using SynologyWebApi;

namespace SynTorrent
{
    /// <summary>
    /// Interaction logic for ConnectWindow.xaml
    /// </summary>
    public partial class ConnectWindow : Elysium.Controls.Window
    {
        public ConnectWindow(DownloadStationApi api)
        {
            InitializeComponent();

            WebApi = api;
            DataContext = WebApi;

            // Make asynchronous connections to be dispatched into the UI thread

            WebApi.QueryApiInfoEvent += QueryApiVersion_FinishedAsync;                
            WebApi.LoginEvent += Login_FinishedAsync;
            WebApi.ProgressEvent += UpdateProgress_Async;

            // Get default settings
            WebApi.Username = Properties.Settings.Default.Username;
            WebApi.Address = Properties.Settings.Default.Address;
            WebApi.UseHTTPS = Properties.Settings.Default.UseHTTPS;
            WebApi.SessionID = Properties.Settings.Default.SessionID;

            WebApi.VerifyNotify();

            // Choose initial focus
            if (WebApi.Address == "")
                Address.Focus();
            else if (WebApi.Username == "")
                LoginName.Focus();
            else
                Password.Focus();

            // Check if we already have a session ID
            if(WebApi.SessionID != "")
            {
                LoginButton_Click(this, new RoutedEventArgs());
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {   
            // Query the API version
            new Task < ApiVersionInfo >(WebApi.QueryApiInfo).Start();

            Address.IsEnabled = false;
            UseHTTPS.IsEnabled = false;
            LoginName.IsEnabled = false;
            Password.IsEnabled = false;
            LoginButton.IsEnabled = false;
        }

        public DownloadStationApi WebApi { get; set; }

        private void Address_TextChanged(object sender, TextChangedEventArgs e)
        {
            WebApi.VerifyNotify();
        }

        private void LoginName_TextChanged(object sender, TextChangedEventArgs e)
        {
            WebApi.VerifyNotify();
        }

        private void QueryApiVersion_FinishedAsync(object sender, QueryApiInfoEventArgs e)
        {
            // Dispatch event coming from a task thread to the main UI thread
            DownloadStationApi.QueryApiInfoHandler queryApi = this.QueryApiVersion_Finished;
            Dispatcher.BeginInvoke(queryApi, sender, e);
        }

        private void QueryApiVersion_Finished(object sender, QueryApiInfoEventArgs e)
        {
            if (!Object.ReferenceEquals(sender, WebApi))
                return;

            this.UpdateUI();

            if (e != null)
            {
                // Successfully queried API version
                System.Console.WriteLine(e.ToString());
                
                // Get password
                WebApi.Password = Password.Password;

                // Log in, create user session
                new Task<bool>(WebApi.Login).Start();
            }
            else
            {
                Address.IsEnabled = true;
                UseHTTPS.IsEnabled = true;
                LoginName.IsEnabled = true;
                Password.IsEnabled = true;
                LoginButton.IsEnabled = true;
                Address.Focus();
            }
        }

        private void Login_FinishedAsync(object sender, ApiRequestResultEventArgs e)
        {
            // Dispatch event coming from a task thread to the main UI thread
            DownloadStationApi.LoginHandler login = this.Login_Finished;
            Dispatcher.BeginInvoke(login, sender, e);
        }

        private void Login_Finished(object sender, ApiRequestResultEventArgs e)
        {
            this.UpdateUI();

            if(e.Success)
            {
                // Successful login, store settings
                Properties.Settings.Default.Username = WebApi.Username;
                Properties.Settings.Default.Address = WebApi.Address;
                Properties.Settings.Default.SessionID = WebApi.SessionID;

                // Close window after 1 sec
                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += TimerTick;
                timer.Start();
                return;
            }

            Address.IsEnabled = true;
            UseHTTPS.IsEnabled = true;
            LoginName.IsEnabled = true;
            Password.IsEnabled = true;
            LoginButton.IsEnabled = true;
            Address.Focus();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            DispatcherTimer timer = (DispatcherTimer)sender;
            timer.Stop();
            timer.Tick -= TimerTick;
            Close();
        }

        private void UpdateUI()
        {
            ConnectionStatusLabel.Text = WebApi.ProgressMessage;
        }

        private void UpdateProgress_Async(object sender, EventArgs e)
        {
            // Dispatch event coming from a task thread to the main UI thread
            DownloadStationApi.ProgressHandler progress = this.UpdateProgress;
            Dispatcher.BeginInvoke(progress, sender, e);
        }

        private void UpdateProgress(object sender, EventArgs e)
        {
            ConnectionStatusLabel.Text = WebApi.ProgressMessage;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Unsubscribe handlers to avoid keeping this Window alive.
            // C# events keep their observers alive!

            WebApi.QueryApiInfoEvent    -= QueryApiVersion_FinishedAsync;
            WebApi.LoginEvent           -= Login_FinishedAsync;
            WebApi.ProgressEvent        -= UpdateProgress_Async;
        }
    }
}
