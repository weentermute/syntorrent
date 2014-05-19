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
        public ConnectWindow(DownloadStationApi api = null)
        {
            InitializeComponent();

            if (api != null)
            {
                WebApi = api;
            }
            else
            {
                WebApi = new DownloadStationApi();
                WebApi.Address = SynTorrent.Properties.Settings.Default.LastConnectServer;
            }
            DataContext = WebApi;

            // Choose initial focus
            if (WebApi.Address == "")
                Address.Focus();
            else if (WebApi.Username == "")
                LoginName.Focus();
            else
                Password.Focus();

            TrustConnectionCheckBox.IsEnabled = UseHTTPS.IsChecked.HasValue ? (bool)UseHTTPS.IsChecked : false;

            // Check if we already have a session ID
            if(WebApi.SessionID != "")
            {
                LoginButton_Click(this, new RoutedEventArgs());
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {   
            // Query the API version
            var version_task = WebApi.QueryApiInfoAsync();

            // Disable UI
            Address.IsEnabled = false;
            UseHTTPS.IsEnabled = false;
            LoginName.IsEnabled = false;
            Password.IsEnabled = false;
            LoginButton.IsEnabled = false;
            TrustConnectionCheckBox.IsEnabled = false;

            // Wait for response
            ApiVersionInfo info = await version_task;

            if(info != null)
            {
                // Successfully queried API version
                System.Console.WriteLine(info.ToString());

                // Get password
                WebApi.Password = Password.Password;

                // Log in, wait for response and create user session
                bool success = await WebApi.LoginAsync();

                if (success)
                {
                    // Successful login, store settings
                    WebApi.Password = "";
                    App.SessionManager.Sessions.AddSession(WebApi);

                    // Close window after 1 sec
                    DispatcherTimer timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(1);
                    timer.Tick += TimerTick;
                    timer.Start();

                    // Remember server in settings
                    SynTorrent.Properties.Settings.Default.LastConnectServer = Address.Text;
                    return;
                }
            }

            // Login failed, enable UI
            Address.IsEnabled = true;
            UseHTTPS.IsEnabled = true;
            LoginName.IsEnabled = true;
            Password.IsEnabled = true;
            LoginButton.IsEnabled = true;
            TrustConnectionCheckBox.IsEnabled = UseHTTPS.IsChecked.HasValue ? (bool)UseHTTPS.IsChecked : false;
            Address.Focus();
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

        private void TimerTick(object sender, EventArgs e)
        {
            DispatcherTimer timer = (DispatcherTimer)sender;
            timer.Stop();
            timer.Tick -= TimerTick;
            Close();
        }

        private void UseHTTPS_Checked(object sender, RoutedEventArgs e)
        {
            TrustConnectionCheckBox.IsEnabled = true;
        }

        private void UseHTTPS_Unchecked(object sender, RoutedEventArgs e)
        {
            TrustConnectionCheckBox.IsEnabled = false;
        }
    }
}
