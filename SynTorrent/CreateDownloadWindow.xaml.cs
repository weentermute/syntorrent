using SynologyWebApi;
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
        public CreateDownloadWindow()
        {
            InitializeComponent();

            UrlTextBox.Focus();

            CanCreate = UrlTextBox.Text != "" || UploadFiles.Count > 0;

            UploadFilesListBox.ItemsSource = UploadFiles;

            // Populate account box
            AccountComboBox.ItemsSource = App.SessionManager.Sessions;
            if (App.SessionManager.Sessions.Count > 0)
            {
                AccountComboBox.SelectedItem = App.SessionManager.Sessions[0];
                DataContext = AccountComboBox.SelectedItem as DownloadStationApi;
            }

            if(String.IsNullOrEmpty(UrlTextBox.Text))
            {
                // Check if clipboard contains a valid Uri string.
                if(Clipboard.ContainsText())
                {
                    string text = Clipboard.GetText();
                    if(IsValidUri(text))
                    {
                        UrlTextBox.Text = text;
                    }
                }
            }

            if (!String.IsNullOrEmpty(UrlTextBox.Text))
            {
                UrlTextBox.SelectAll();
            }

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

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            var connection = AccountComboBox.SelectedItem as ConnectionViewModel;
            if (connection == null)
                return;

            CreateButton.IsEnabled = false;

            bool success = false;

            if(UrlTextBox.Text != "")
            {
                var uri = UrlTextBox.Text;
                success = success && await App.SessionManager.CreateDownloadTaskAsync(uri, connection.ConnectionId);
            }
            if (UploadFiles.Count > 0)
            {
                foreach(var item in UploadFiles)
                {
                    var file = (string)item;
                    success = success && await App.SessionManager.CreateDownloadTaskFromFileAsync(file, connection.ConnectionId);
                }
            }

            CreateButton.IsEnabled = true;
            if (success && IsVisible)
                this.Close();
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

        static private Boolean IsValidUri(String uri)
        {
            try
            {
                new Uri(uri);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void AccountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var connection = AccountComboBox.SelectedItem as ConnectionViewModel;
            if (connection != null)
            {
                DataContext = connection.Session; 
            }
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
