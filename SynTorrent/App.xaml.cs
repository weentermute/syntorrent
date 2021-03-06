﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Elysium;
using SingleInstanceApplication;
using System.Reflection;
using System.Deployment.Application;
using System.Web;
using SynologyWebApi;

namespace SynTorrent
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Retrieves the current ClickOnce Version.
        /// </summary>
        /// <returns></returns>
        static public System.Version ClickOnceVersion()
        {
            if (ApplicationDeployment.IsNetworkDeployed)
                return ApplicationDeployment.CurrentDeployment.CurrentVersion;
            return new System.Version();
        }

        private void StartupHandler(object sender, StartupEventArgs e)
        {
            // Apply Elysium theme
            Elysium.Manager.Apply(this, Elysium.Theme.Dark, Elysium.AccentBrushes.Blue, System.Windows.Media.Brushes.White);
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            SynTorrent.Properties.Settings.Default.AccountList.Clear();
            SynTorrent.Properties.Settings.Default.AccountList.AddRange(SessionManager.Accounts);

            // Save user settings
            SynTorrent.Properties.Settings.Default.Save();
        }

        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            // Register single instance application and check for existence of other process
            if (!ApplicationInstanceManager.CreateSingleInstance(
                    Assembly.GetExecutingAssembly().GetName().Name,
                    SingleInstanceCallback))
            {
                // exit, if same application is running
                Environment.Exit(0);
                return; 
            }
            base.OnStartup(e);

            // Prepare session manager
            // Get default settings
            AccountList accounts = SynTorrent.Properties.Settings.Default.AccountList;
            if (accounts == null)
            {
                SynTorrent.Properties.Settings.Default.AccountList = new AccountList();
            }
            SessionManager.Initialize(SynTorrent.Properties.Settings.Default.AccountList);
        }

        /// <summary>
        /// Single instance callback handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="SingleInstanceApplication.InstanceCallbackEventArgs"/> instance containing the event data.</param>
        private void SingleInstanceCallback(object sender, InstanceCallbackEventArgs args)
        {
            if (args == null || Dispatcher == null) return;
            Action<bool> d = (bool x) =>
            {
                var win = MainWindow as MainWindow;
                if (win == null) return;

                win.Activate(x);
                win.ApendArgs(args.CommandLineArgs);

                // Arguments have been processed, allow other process to exit.
                SingleInstanceApplication.ApplicationInstanceManager.DoneProcessingArgs();
            };
            Dispatcher.Invoke(d, true);
        }

        static public DownloadStationManager SessionManager = new DownloadStationManager();
    }
}
