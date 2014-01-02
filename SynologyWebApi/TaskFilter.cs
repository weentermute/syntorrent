using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SynologyWebApi
{
    /// <summary>
    /// Abstract filter base class for download tasks.
    /// </summary>
    public abstract class TaskFilter
    {
        /// <summary>
        /// Override this method to implement the filter.
        /// </summary>
        /// <param name="task"></param>
        /// <returns>true if filter accepts task</returns>
        abstract public bool AcceptThis(DownloadTask task);

        /// <summary>
        /// Helper function which implements a case insensitive string.Contains function.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="toCheck"></param>
        /// <param name="comp"></param>
        /// <returns></returns>
        public static bool Contains(string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }

        private TaskFilterCollection ChildrenValue = new TaskFilterCollection();

        /// <summary>
        /// Provides a list of child filters which are required to fulfill this filter.
        /// </summary>
        public TaskFilterCollection Children
        {
            get { return ChildrenValue;  }
        }

        public void AddFilter(TaskFilter filter)
        {
            filter.Parent = this;
            Children.Add(filter);
        }

        /// <summary>
        /// Tests whether a task fulfills this filter including all its parents.
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public bool Accept(DownloadTask task)
        {
            TaskFilter filter = this;
            while(filter != null)
            {
                if (!filter.AcceptThis(task))
                    return false;
                filter = filter.Parent;
            }
            return true;
        }

        public abstract string Name
        {
            get;
        }

        public virtual Color FilterColor
        {
            get
            {
                return Colors.Transparent;
            }
        }

        public TaskFilter Parent;
    }

    /// <summary>
    /// File name filter.
    /// </summary>
    public class FileNameTaskFilter : TaskFilter
    {
        public string QueryString
        {
            get
            {
                return QueryStringValue;
            }
            set
            {
                QueryStringValue = value;
                // Split into search keywords
                SearchWords = value.Split(' ');
            }
        }

        override public bool AcceptThis(DownloadTask task)
        {
            if (QueryString == "")
                return true;
            
            string fileName = task.File;

            // Test if all words are contained.
            bool b = SearchWords.All( (string s) => Contains(fileName, s, StringComparison.OrdinalIgnoreCase) );
            return b;
        }

        public override string Name
        {
            get { return "Filename";  }
        }

        private string QueryStringValue = "";
        private string[] SearchWords;
    }

    /// <summary>
    /// Status based task filter
    /// </summary>
    public class StatusTaskFilter : TaskFilter
    {
        public StatusTaskFilter(string status)
        {
            Status = status;
        }

        override public bool AcceptThis(DownloadTask task)
        {
            if (Status == "")
                return true;

            return task.Status == Status;
        }

        public override string Name
        {
            get 
            {
                System.Globalization.TextInfo ti = System.Globalization.CultureInfo.CurrentCulture.TextInfo;
                return ti.ToTitleCase(Status);  
            }
        }

        public override Color FilterColor
        {
            get
            {
                return DownloadTask.GetStateColor(Status);
            }
        }

        public readonly string Status = "";
    }

    /// <summary>
    /// Filters in all tasks currently uploading.
    /// </summary>
    public class UploadingTaskFilter : TaskFilter
    {
        override public bool AcceptThis(DownloadTask task)
        {
            return task.UploadSpeed.SizeBytes > 0;
        }

        public override string Name
        {
            get { return "Uploading"; }
        }

        public override Color FilterColor
        {
            get
            {
                return DownloadTask.GetStateColor("seeding");
            }
        }
    }

    /// <summary>
    /// Root node of task filter tree.
    /// </summary>
    public class RootTaskFilter : TaskFilter
    {
        public RootTaskFilter()
        { }

        override public bool AcceptThis(DownloadTask task)
        {
            return true;
        }

        public override string Name
        {
            get { return "All"; }
        }
    }
}
