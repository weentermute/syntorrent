﻿using SynologyWebApi;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SynTorrent
{
    /// <summary>
    /// Interaction logic for CreateDownloadWindow.xaml
    /// </summary>
    public partial class CreateDownloadWindow : Elysium.Controls.Window
    {
        public DownloadStationApi WebApi { get; set; }

        public CreateDownloadWindow(DownloadStationApi api)
        {
            InitializeComponent();

            WebApi = api;
            DataContext = WebApi;

            WebApi.CreateDownloadTaskEvent += CreateTask_FinishedAsync;

            WebApi.ProgressMessage = "";

            UrlTextBox.Focus();

            CanCreate = UrlTextBox.Text != "" || UploadFiles.Count > 0;

            UploadFilesListBox.ItemsSource = UploadFiles;

            Activate();
        }

        // Dependency Property
        public static readonly DependencyProperty CanCreateProperty =
             DependencyProperty.Register("CanCreate", typeof(Boolean),
             typeof(CreateDownloadWindow));

        /// <summary>
        /// List of files to be uploaded to the DownloadStation.
        /// </summary>
        public ObservableCollection<string> UploadFiles = new ObservableCollection<string>();

        /// <summary>
        /// Holds true if a download task can be created.
        /// </summary>
        public bool CanCreate
        {
            get { return (bool)GetValue(CanCreateProperty); }
            set { SetValue(CanCreateProperty, value); }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            CreateButton.IsEnabled = false;           
            if(UrlTextBox.Text != "")
            {
                var uri = UrlTextBox.Text;
                var task = new Task<bool>(() => WebApi.CreateDownloadTask(uri));
                task.Start();
            }
            if (UploadFiles.Count > 0)
            {
                foreach(var item in UploadFiles)
                {
                    var file = (string)item;
                    var task = new Task<bool>(() => WebApi.CreateDownloadTaskFromFile(file));
                    task.Start();
                }
            }
        }

        private void CreateTask_FinishedAsync(object sender, ApiRequestResultEventArgs e)
        {
            DownloadStationApi.CreateDownloadTaskHandler createTask = this.CreateTask_Finished;
            Dispatcher.BeginInvoke(createTask, sender, e);
        }

        private void CreateTask_Finished(object sender, ApiRequestResultEventArgs e)
        {
            if(e.Success)
            {
                if(IsVisible)
                    this.Close();
                WebApi.VerifyNotify();
            }
            CreateButton.IsEnabled = true;
        }

        private void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WebApi.CreateDownloadTaskEvent -= CreateTask_FinishedAsync;
        }

        private void SpecifyFileButton_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = ""; // Default file name
            dlg.DefaultExt = ".torrent"; // Default file extension
            dlg.Filter = "Downloadable Files (.torrent)|*.torrent|All Files|*.*"; // Filter files by extension 
            dlg.Multiselect = true;

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                // Process
                foreach(string fileName in dlg.FileNames)
                {
                    UploadFiles.Add(fileName);
                }
            }

            CanCreate = UrlTextBox.Text != "" || UploadFiles.Count > 0;
        }

        private void UrlTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CanCreate = UrlTextBox.Text != "" || UploadFiles.Count > 0;
        }

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            CanCreate = UrlTextBox.Text != "" || UploadFiles.Count > 0;
        } 
    }

    /// <summary>
    /// Simple value converter which extracts the file name from a file path.
    /// </summary>
    [ValueConversion(typeof(string), typeof(String))]
    public class FilePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string filePath = (string)value;
            return Path.GetFileName(filePath);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
