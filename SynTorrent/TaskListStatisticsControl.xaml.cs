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
    /// Interaction logic for TaskListStatisticsControl.xaml
    /// </summary>
    public partial class TaskListStatisticsControl : UserControl
    {
        public TaskListStatisticsControl()
        {
            InitializeComponent();

            _Statistics = (TaskListStatistics)DataContext;
        }

        public async void UpdateStatsAsync( IList<DownloadTask> tasks)
        {
            await _Statistics.UpdateAsync(tasks);
        }

        private TaskListStatistics _Statistics; 
    }
}
