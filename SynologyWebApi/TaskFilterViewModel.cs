using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SynologyWebApi
{
    /// <summary>
    /// View Model for a task filter item.
    /// </summary>
    public class TaskFilterViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filter"></param>
        public TaskFilterViewModel(TaskFilter filter)
            : this(filter, null)
        {

        }

        /// <summary>
        /// Pretty name of a filter
        /// </summary>
        public string Name
        {
            get { return String.Format("{0} ({1})", _filter.Name, _taskCount); }
        }

        /// <summary>
        /// Child filters attached to this filter.
        /// </summary>
        public ObservableCollection<TaskFilterViewModel> Children
        {
            get { return _children;  }
        }

        /// <summary>
        /// Selection state.
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    this.OnPropertyChanged("IsSelected");
                }
            }
        }

        /// <summary>
        /// Expand state
        /// </summary>
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged("IsExpanded");
                }

                // Expand all the way up to the root.
                if (_isExpanded && _parent != null)
                    _parent.IsExpanded = true;
            }
        }

        /// <summary>
        /// Task filter held by this object.
        /// </summary>
        public TaskFilter Filter
        {
            get { return _filter; }
        }

        /// <summary>
        /// Task filter color.
        /// </summary>
        public Brush FilterBrush
        {
            get
            {
                return new SolidColorBrush(_filter.FilterColor);
            }
        }

        /// <summary>
        /// Holds the number of download tasks which fulfill this filter node.
        /// </summary>
        public int TaskCount
        {
            get
            {
                return _taskCount;
            }
        }

        /// <summary>
        /// Updates the task count of this node and its children.
        /// </summary>
        public void UpdateTaskCount(TaskCollection tasks)
        {
            int count = tasks.Count<DownloadTask>((DownloadTask t) => _filter.AcceptThis(t));
            foreach (var f in _children)
                f.UpdateTaskCount(tasks);
            if(count != _taskCount)
            {
                _taskCount = count;
                this.OnPropertyChanged("Name");
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion // INotifyPropertyChanged Members

        #region Private Members

        private TaskFilterViewModel(TaskFilter filter, TaskFilterViewModel parent)
        {
            _filter = filter;
            _parent = parent;

            _children = new ObservableCollection<TaskFilterViewModel>(
                    (from child in _filter.Children
                     select new TaskFilterViewModel(child, this))
                     .ToList<TaskFilterViewModel>());
        }

        private TaskFilter _filter;
        private TaskFilterViewModel _parent;
        private ObservableCollection<TaskFilterViewModel> _children;
        private bool _isSelected = false;
        private bool _isExpanded = true;
        private int _taskCount = 0;

        #endregion
    }
}
