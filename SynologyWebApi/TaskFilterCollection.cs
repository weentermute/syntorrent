using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynologyWebApi
{
    /// <summary>
    /// An observable collection of filters to be applied to a download task collection.
    /// </summary>
    public class TaskFilterCollection : ObservableCollection<TaskFilter>
    {
        public bool Accept(DownloadTask task)
        {
            if(Count > 0)
                return this.All((TaskFilter f) => f.Accept(task));
            return true;
        }
    }
}
