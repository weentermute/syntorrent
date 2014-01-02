using SynologyWebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SynologyWebApi
{
    /// <summary>
    /// Provides a filtered view on a task collection.
    /// </summary>
    public class TaskCollectionFilterView : CollectionViewSource
    {
        /// <summary>
        /// Construct given a task collection to be filtered given a default root filter.
        /// </summary>
        /// <param name="tasks"></param>
        public TaskCollectionFilterView(TaskCollection tasks, TaskFilterCollection filters)
        {
            TaskFiltersValue = filters;
            Source = tasks;
            Filter += viewSource_Filter;
        }

        /// <summary>
        /// Task filters applied to the task collection.
        /// </summary>
        public TaskFilterCollection TaskFilters
        {
            get
            {
                return TaskFiltersValue;
            }
            set
            {
                TaskFiltersValue = value;
                View.Refresh();
            }
        }

        private void viewSource_Filter(object sender, FilterEventArgs e)
        {
            DownloadTask task = e.Item as DownloadTask;
            e.Accepted = TaskFiltersValue.Accept(task);
        }

        private TaskFilterCollection TaskFiltersValue;
    }
}
