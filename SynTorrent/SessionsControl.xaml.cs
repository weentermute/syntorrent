using SynologyWebApi;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SynTorrent
{
    /// <summary>
    /// Interaction logic for SessionsConrol.xaml
    /// </summary>
    public partial class SessionsControl : UserControl
    {
        public SessionsControl()
        {
            InitializeComponent();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            ConnectWindow win = new ConnectWindow();
            win.ShowDialog();
        }

        private async void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveButton.IsEnabled = false;
            var items = ConnectionsList.SelectedItems;
            List<ConnectionViewModel> list = new List<ConnectionViewModel>();
            foreach( var item in items)
            {
                ConnectionViewModel connection = item as ConnectionViewModel;
                if( connection != null)
                {
                    await connection.Session.LogoutAsync();
                    list.Add(connection);
                }
            }

            foreach (var item in list)
                App.SessionManager.Sessions.Remove(item);

            RemoveButton.IsEnabled = ConnectionsList.SelectedItems.Count > 0;
        }

        private void ConnectionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RemoveButton.IsEnabled = ConnectionsList.SelectedItems.Count > 0;
        }
    }
}
