using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using SynologyWebApi;
using System.Windows.Threading;
using System.Text.RegularExpressions;

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

            WebApi = (DownloadStationApi)DataContext;

            DownloadStationApi.GetTaskListHandler queryList = this.GetTaskList_Finished;
            WebApi.GetTaskListEvent += (object sender, ApiRequestResultEventArgs e) => Dispatcher.BeginInvoke(queryList, sender, e);

            DownloadStationApi.LoginHandler login = this.Login_Finished;
            WebApi.LoginEvent += (object sender, ApiRequestResultEventArgs e) => Dispatcher.BeginInvoke(login, sender, e);

            // Initialize the filter task view
            TaskCollectionFilterViewValue = new TaskCollectionFilterView(TaskCollectionValue, FilterControl.ActiveFilters);

            RefreshListTimer.Interval = TimeSpan.FromSeconds(1);
            RefreshListTimer.Tick += RefreshListTimerTick;

            // Bind list of session with list view
            SessionsControl.ConnectionsList.ItemsSource = App.SessionManager.Sessions;

            // Show login on startup
            // ShowLogin();

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

            // Check command line options
            ApendArgs(SingleInstanceApplication.CommandLineArguments.GetArguments());

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

        private void RefreshListTimerTick(object sender, EventArgs e)
        {
            if (WebApi.IsConnected && WebApi.IsIdle)
                GetTaskList_Start(this);
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            // Log out
            WebApi.Logout();
            Properties.Settings.Default.SessionID = "";
            ShowLogin();
        }

        private void Login_Finished(object sender, ApiRequestResultEventArgs e)
        {
            if(e.Success)
            {
                // Login successful, start retrieving task list
                GetTaskList_Start(this);
            }
        }

        private void GetTaskList_Start(object sender)
        {            
            GetListTask = new Task<TaskCollection>(WebApi.GetTaskList);
            GetListTask.Start();
        }

        private void GetTaskList_Finished(object sender, ApiRequestResultEventArgs e)
        {
            var result = GetListTask.Result;
            if (result != null)
            {
                // Update main list with changed download tasks
                TaskCollection.UpdateWith(result);
                WebApi.VerifyNotify();

                // Update stats
                UpdateStatistics();

                // Update filter tree
                FilterControl.FiltersTreeViewModel.UpdateTaskCount(TaskCollection);
            }
        }

        private void UpdateStatistics()
        {
            List<DownloadTask> tasks = new List<DownloadTask>();
            foreach (var item in TaskList.Items)
                tasks.Add((DownloadTask)item);
            StatsControl.UpdateStatsAsync(tasks);
        }

        /// Web session interface
        private DownloadStationApi WebApi;
        private Task<TaskCollection> GetListTask;
        private TaskCollection TaskCollectionValue = new TaskCollection();
        private TaskCollectionFilterView TaskCollectionFilterViewValue;

        /// <summary>
        /// Collection of download tasks
        /// </summary>
        public TaskCollection TaskCollection
        {
            get
            {
                return TaskCollectionValue;
            }
            set
            {
                TaskCollectionValue = value;
            }
        }

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

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if( WebApi.SessionID != "")
                GetTaskList_Start(this);
        }

        private static Regex _RegExCamelCase = new Regex("([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))");

        private void TaskList_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // Adjust and rename some auto generated columns
            string headerName = e.Column.Header.ToString();

            if( headerName == "Id" )
            {
                // Hide Id column
                e.Cancel = true;
            }
            else if( headerName == "TaskStateColor")
            {
                e.Cancel = true;
            }
            else if( headerName == "Ratio")
            {
                (e.Column as DataGridTextColumn).Binding.StringFormat = "{0:F2}";
            }
            else if (headerName == "Progress")
            {
                (e.Column as DataGridTextColumn).Binding.StringFormat = "{0:F1}%";
            }

            // Convert from "CamelCase" to "Camel Case"
            headerName = _RegExCamelCase.Replace(headerName, "$1 ");

            e.Column.Header = headerName;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            CreateDownloadWindow createWindow = new CreateDownloadWindow(WebApi);
            createWindow.Owner = this;
            createWindow.ShowDialog();
        }

        private void TitleBarLoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Log out
            WebApi.Logout();
            Properties.Settings.Default.SessionID = "";
            ShowLogin();
        }

        private void TitleBarSettingsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void FilterControl_SearchFieldEdited(object sender, RoutedEventArgs e)
        {
            // Update default search filter            
            TaskCollectionView.View.Refresh();
        }

        private ConnectWindow ConnectWindow;

        private void ShowLogin()
        {
            RefreshListTimer.Stop();
            this.ConnectWindow = new ConnectWindow(WebApi);
            ConnectWindow.ShowDialog();
            this.ConnectWindow = null;
            RefreshListTimer.Start();
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
            if( this.WindowState == System.Windows.WindowState.Minimized)
            {
                if(RefreshListTimer.IsEnabled)
                    RefreshListTimer.Stop();
            }
            else
            {
                if (!RefreshListTimer.IsEnabled)
                    RefreshListTimer.Start();
            }
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

                // Make sure we're logged in
                if (WebApi.SessionID == "")
                    ShowLogin();

                if (WebApi.SessionID != "")
                {
                    CreateDownloadWindow createWindow = new CreateDownloadWindow(WebApi);
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
                return TaskList.SelectedItems.Count > 0;
            }
        }

        public bool CanResume
        {
            get
            {
                return TaskList.SelectedItems.Count > 0;
            }
        }

        public bool CanDelete
        {
            get
            {
                return TaskList.SelectedItems.Count > 0;
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            // Get list of selected data items
            var selection = TaskList.SelectedItems;

            List<DownloadTask> tasks = new List<DownloadTask>();
            foreach( var item in selection)
            {
                DownloadTask task = item as DownloadTask;
                if(task != null)
                    tasks.Add(task);
            }

            if (tasks.Count > 0)
            {
                WebApi.PauseDownloadTasks(tasks);
            }
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            // Get list of selected data items
            var selection = TaskList.SelectedItems;

            List<DownloadTask> tasks = new List<DownloadTask>();
            foreach (var item in selection)
            {
                DownloadTask task = item as DownloadTask;
                if (task != null)
                    tasks.Add(task);
            }

            if (tasks.Count > 0)
            {
                WebApi.ResumeDownloadTasks(tasks);
            }
        }

        private void TaskList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ResumeButton.IsEnabled = CanResume;
            PauseButton.IsEnabled = CanPause;
            DeleteButton.IsEnabled = CanDelete;
        }

        private bool DeleteSelectedTasks()
        {
            // Get list of selected data items
            var selection = TaskList.SelectedItems;

            string msg;

            if (selection.Count > 1)
            {
                msg = String.Format("Are you sure you want to delete {0} tasks?", selection.Count);
            }
            else if (selection.Count == 1)
            {
                msg = String.Format("Are you sure you want to delete {0}?", (selection[0] as DownloadTask).File);
            }
            else
            {
                return false;
            }

            var result = MessageWindow.Show(msg, "Delete Tasks", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                // Ask user before deleting any tasks
                List<DownloadTask> tasks = new List<DownloadTask>();
                foreach (var item in selection)
                {
                    DownloadTask task = item as DownloadTask;
                    if (task != null)
                        tasks.Add(task);
                }

                if (tasks.Count > 0)
                {
                    WebApi.DeleteDownloadTasks(tasks);
                    return true;
                }
            }

            return false;
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
    }
}
