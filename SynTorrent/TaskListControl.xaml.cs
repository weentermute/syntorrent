using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace SynTorrent
{
    /// <summary>
    /// Interaction logic for TaskListControl.xaml
    /// </summary>
    public partial class TaskListControl : UserControl
    {
        public TaskListControl()
        {
            InitializeComponent();
        }

        private static Regex _RegExCamelCase = new Regex("([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))");

        private void TaskList_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // Adjust and rename some auto generated columns
            string headerName = e.Column.Header.ToString();

            if (headerName == "Id" || headerName == "UniqueId")
            {
                // Hide Id column
                e.Cancel = true;
                return;
            }
            else if (headerName == "TaskStateColor")
            {
                e.Cancel = true;
                return;
            }
            else if (headerName == "Ratio")
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
    }
}
