﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using SynologyWebApi;
using System.Windows.Threading;

namespace SynTorrent
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Elysium.Controls.Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Initialize the filter task view
            TaskCollectionFilterViewValue = new TaskCollectionFilterView(App.SessionManager.AllTasks, FilterControl.ActiveFilters);

            RefreshListTimer.Interval = TimeSpan.FromSeconds(1);
            RefreshListTimer.Tick += RefreshListTimerTick;

            // Bind list of connections with list box control
            SessionsControl.ConnectionsList.ItemsSource = App.SessionManager.Sessions;

            // Load layout
            var userPrefs = Properties.Settings.Default;
            this.Height = userPrefs.WindowHeight;
            this.Width = userPrefs.WindowWidth;
            this.Top = userPrefs.WindowTop;
            this.Left = userPrefs.WindowLeft;
            this.WindowState = userPrefs.WindowState;

            // Size it to fit the current screen
            SizeToFit();

            // Move the window at least partially into view
            MoveIntoView();

            // Start periodic refresh
            RefreshListTimer.Start();

            // Store window state
            PreviouseWindowState = WindowState;

            // Attach to event to remember previous window state in case the window gets activated
            LayoutUpdated += Window_LayoutUpdated;

            // Connect events from data grid
            TaskListControl.TaskList.SelectionChanged += TaskList_SelectionChanged;
            TaskListControl.TaskList.PreviewKeyDown += TaskList_PreviewKeyDown;

            // Connect data source for task list
            TaskListControl.TaskList.ItemsSource = TaskCollectionFilterViewValue.View;

            // Set version info
            if (App.ClickOnceVersion() != new System.Version())
                Title += " " + App.ClickOnceVersion().ToString();

            // Start login tasks
            StartLoginTasksAsync();
        }

        private async void StartLoginTasksAsync()
        {
            await App.SessionManager.LoginAsync();
        }

        private async Task UpdateAllTasksAsync()
        {
            // This delays automatic refresh until the defer cycle is exited.
            using(TaskCollectionFilterViewValue.DeferRefresh())
            {
                await App.SessionManager.CollectAllTasksAsync();
            }

            // Update stats
            UpdateStatistics();

            // Update filter tree
            FilterControl.FiltersTreeViewModel.UpdateTaskCount(App.SessionManager.AllTasks);
        }

        private async void RefreshListTimerTick(object sender, EventArgs e)
        {
            // Stop timer to prevent accumulating concurrent web API queries
            RefreshListTimer.Stop();

            await UpdateAllTasksAsync();

            // Done updating, schedule next timer tick
            RefreshListTimer.Start();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            ShowLogin();
        }

        private void UpdateStatistics()
        {
            List<DownloadTask> tasks = new List<DownloadTask>();
            foreach (var item in TaskListControl.TaskList.Items)
                tasks.Add((DownloadTask)item);
            StatsControl.UpdateStatsAsync(tasks);
        }

        private TaskCollectionFilterView TaskCollectionFilterViewValue;

        public TaskCollectionFilterView TaskCollectionView
        {
            get
            {
                return TaskCollectionFilterViewValue;
            }
            set
            {
                TaskCollectionFilterViewValue = value;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            CreateDownloadWindow createWindow = new CreateDownloadWindow();
            createWindow.Owner = this;
            createWindow.ShowDialog();
        }

        private void TitleBarSettingsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void FilterControl_SearchFieldEdited(object sender, RoutedEventArgs e)
        {
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            // Update default search filter            
            TaskCollectionView.View.Refresh();

            stopWatch.Stop();

            TimeSpan ts = stopWatch.Elapsed;
            System.Console.WriteLine("Filter Time = {0:00} ms", ts.TotalMilliseconds);
        }

        private ConnectWindow ConnectWindow;

        private void ShowLogin()
        {
            if(this.ConnectWindow == null)
            {
                this.ConnectWindow = new ConnectWindow();
                ConnectWindow.ShowDialog();
                this.ConnectWindow = null;
            }
        }

        private DispatcherTimer RefreshListTimer = new DispatcherTimer();

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            RefreshListTimer.Stop();

            var userPrefs = Properties.Settings.Default;

            userPrefs.WindowHeight = this.Height;
            userPrefs.WindowWidth = this.Width;
            userPrefs.WindowTop = this.Top;
            userPrefs.WindowLeft = this.Left;
            userPrefs.WindowState = this.WindowState;

            userPrefs.CurrentFilterKeywords = FilterControl.SearchBox.TextBox.Text;
        }

        public void SizeToFit()
        {
            if (Height > System.Windows.SystemParameters.VirtualScreenHeight)
            {
                Height = System.Windows.SystemParameters.VirtualScreenHeight;
            }

            if (Width > System.Windows.SystemParameters.VirtualScreenWidth)
            {
                Width = System.Windows.SystemParameters.VirtualScreenWidth;
            }
        }

        public void MoveIntoView()
        {
            if (Top + Height / 2 >
                 System.Windows.SystemParameters.VirtualScreenHeight)
            {
                Top =
                  System.Windows.SystemParameters.VirtualScreenHeight - Height;
            }

            if (Left + Width / 2 >
                     System.Windows.SystemParameters.VirtualScreenWidth)
            {
                Left =
                  System.Windows.SystemParameters.VirtualScreenWidth - Width;
            }

            if (Top < 0)
            {
                Top = 0;
            }

            if (Left < 0)
            {
                Left = 0;
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
        }

        public void ApendArgs(string[] args)
        {
            if(args.Length>1)
            {
                Activate();

                // When deploying a ClickOnce application the first argument will a .application file, we don't
                // want that!!
                if (args[1].EndsWith(".application"))
                    return;

                // Make sure we have at least one account
                if (App.SessionManager.Sessions.Count == 0)
                    ShowLogin();

                if (App.SessionManager.Sessions.Count > 0)
                {
                    CreateDownloadWindow createWindow = new CreateDownloadWindow();
                    createWindow.UploadFiles.Add(args[1]);
                    createWindow.ShowDialog();
                }
            }
        }

        /// <summary>
        /// Gets Previous Window State.
        /// </summary>
        public WindowState PreviouseWindowState { get; private set; }

        /// <summary>
        /// Activates Window <para/>
        /// Example for this: http://blogs.microsoft.co.il/blogs/maxim/archive/2009/12/24/daily-tip-how-to-activate-minimized-window-form.aspx
        /// </summary>
        /// <param name="restoreIfMinimized">if [true] restore prev. win. state</param>
        /// <returns></returns>
        public bool Activate(bool restoreIfMinimized)
        {
            if (restoreIfMinimized && WindowState == WindowState.Minimized)
            {
                WindowState = PreviouseWindowState == WindowState.Normal
                                        ? WindowState.Normal : WindowState.Maximized;
            }
            return Activate();
        }

        /// <summary>
        /// Occurs on layout change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_LayoutUpdated(object sender, EventArgs e)
        {
            PreviouseWindowState = WindowState;
        }

        public bool CanPause
        {
            get
            {
                return TaskListControl.TaskList.SelectedItems.Count > 0;
            }
        }

        public bool CanResume
        {
            get
            {
                return TaskListControl.TaskList.SelectedItems.Count > 0;
            }
        }

        public bool CanDelete
        {
            get
            {
                return TaskListControl.TaskList.SelectedItems.Count > 0;
            }
        }

        private async void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            // Get list of selected data items
            var selection = TaskListControl.TaskList.SelectedItems;

            List<DownloadTask> tasks = new List<DownloadTask>();
            foreach( var item in selection)
            {
                DownloadTask task = item as DownloadTask;
                if(task != null)
                    tasks.Add(task);
            }

            if (tasks.Count > 0)
            {
                await App.SessionManager.PauseDownloadTasksAsync(tasks);
            }
        }

        private async void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            // Get list of selected data items
            var selection = TaskListControl.TaskList.SelectedItems;

            List<DownloadTask> tasks = new List<DownloadTask>();
            foreach (var item in selection)
            {
                DownloadTask task = item as DownloadTask;
                if (task != null)
                    tasks.Add(task);
            }

            if (tasks.Count > 0)
            {
                await App.SessionManager.ResumeDownloadTasksAsync(tasks);
            }
        }

        private void TaskList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ResumeButton.IsEnabled = CanResume;
            PauseButton.IsEnabled = CanPause;
            DeleteButton.IsEnabled = CanDelete;
        }

        private async void DeleteSelectedTasks()
        {
            // Get list of selected data items
            var selection = TaskListControl.TaskList.SelectedItems;

            string msg;

            if (selection.Count > 1)
            {
                msg = String.Format("Are you sure you want to remove {0} tasks?", selection.Count);
            }
            else if (selection.Count == 1)
            {
                msg = String.Format("Are you sure you want to remove {0}?", (selection[0] as DownloadTask).File);
            }
            else
            {
                return;
            }

            // Ask user before deleting any tasks
            var result = MessageWindow.Show(msg, "Remove Tasks", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                List<DownloadTask> tasks = new List<DownloadTask>();
                foreach (var item in selection)
                {
                    DownloadTask task = item as DownloadTask;
                    if (task != null)
                        tasks.Add(task);
                }

                if (tasks.Count > 0)
                {
                    await App.SessionManager.DeleteDownloadTasksAsync(tasks);
                    return;
                }
            }

            return;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedTasks();
        }

        private void TaskList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Intercept Del key and handle it ourselves
            if (e.Key == Key.Delete)
            {
                DeleteSelectedTasks();
                e.Handled = true;
            }
        }

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            // Check command line options
            ApendArgs(SingleInstanceApplication.CommandLineArguments.GetArguments());

            // Make sure we have at least one account
            if (App.SessionManager.Sessions.Count == 0)
                ShowLogin();
        }
    }
}
