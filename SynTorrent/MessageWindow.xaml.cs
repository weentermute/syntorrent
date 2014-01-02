using System.Windows;

namespace SynTorrent
{
    /// <summary>
    /// Emulates a simple MessageBox like window.
    /// </summary>
    public partial class MessageWindow : Elysium.Controls.Window
    {
        public MessageWindow()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Emulates the MessageBox.Show function.
        /// </summary>
        /// <param name="messageBoxText"></param>
        /// <param name="caption"></param>
        /// <param name="button"></param>
        /// <returns></returns>
        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button)
        {
            MessageWindow win = new MessageWindow();

            win.MessageText.Text = messageBoxText;
            win.Title = caption;

            switch(button)
            {
                case MessageBoxButton.OK:
                    win.Button1.Visibility = Visibility.Hidden;
                    win.Button2.Visibility = Visibility.Hidden;
                    win.Button3.Visibility = Visibility.Visible;
                    win.Button3.Content = "Ok";
                    win._messageButtons[2] = MessageBoxResult.OK;
                    break;
                case MessageBoxButton.OKCancel:
                    win.Button1.Visibility = Visibility.Hidden;
                    win.Button2.Visibility = Visibility.Visible;
                    win.Button3.Visibility = Visibility.Visible;
                    win.Button2.Content = "Ok";
                    win.Button3.Content = "Cancel";
                    win._messageButtons[1] = MessageBoxResult.OK;
                    win._messageButtons[2] = MessageBoxResult.Cancel;
                    break;
                case MessageBoxButton.YesNo:
                    win.Button1.Visibility = Visibility.Hidden;
                    win.Button2.Visibility = Visibility.Visible;
                    win.Button3.Visibility = Visibility.Visible;
                    win.Button2.Content = "Yes";
                    win.Button3.Content = "No";
                    win._messageButtons[1] = MessageBoxResult.Yes;
                    win._messageButtons[2] = MessageBoxResult.No;
                    break;
                case MessageBoxButton.YesNoCancel:
                    win.Button1.Visibility = Visibility.Visible;
                    win.Button2.Visibility = Visibility.Visible;
                    win.Button3.Visibility = Visibility.Visible;
                    win.Button1.Content = "Yes";
                    win.Button2.Content = "No";
                    win.Button3.Content = "Cancel";
                    win._messageButtons[0] = MessageBoxResult.Yes;
                    win._messageButtons[1] = MessageBoxResult.No;
                    win._messageButtons[2] = MessageBoxResult.Cancel;
                    break;
            }

            var accepted = win.ShowDialog();

            return win._messageResult;
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this._messageResult = _messageButtons[0];
            this.Close();
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this._messageResult = _messageButtons[1];
            this.Close();
        }

        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this._messageResult = _messageButtons[2];
            this.Close();
        }

        private MessageBoxResult[] _messageButtons = new MessageBoxResult[3]{MessageBoxResult.Cancel, MessageBoxResult.Cancel, MessageBoxResult.Cancel};
        private MessageBoxResult _messageResult = MessageBoxResult.Cancel;
    }
}
