using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SynologyWebApi
{
    /// <summary>
    /// An observable collection of download tasks.
    /// </summary>
    public class TaskCollection : ObservableCollection<DownloadTask>
    {
        /// <summary>
        /// Performs an update of this collection against another collection
        /// by touching only the tasks which have changed.
        /// 
        /// </summary>
        /// <param name="other"></param>
        public void UpdateWith(TaskCollection other)
        {
            CreateIndexing();
            other.CreateIndexing();
            var thisIds = TaskIndexing.Keys;
            List<DownloadTask> toRemove = new List<DownloadTask>();

            int added = 0;
            int modified = 0;
            int removed = 0;

            foreach(string id in thisIds )
            {
                DownloadTask otherItem = other.FindTask(id);
                if(otherItem != null)
                {
                    DownloadTask thisItem = FindTask(id);
                    // Compare data
                    if( !(thisItem.Equals(otherItem) ) )
                    {
                        // Data changed, replace
                        Int32 index = IndexOf(thisItem);                        
                        SetItem(index, otherItem);
                        modified++;
                    }
                }
                else
                {
                    // Remove
                    DownloadTask thisItem = FindTask(id);
                    toRemove.Add(thisItem);
                    removed++;
               }
            }

            foreach (DownloadTask otherTask in other)
            {
                if(!TaskIndexing.ContainsKey(otherTask.Id))
                {
                    Add(otherTask);
                    added++;
                }
            }

            // Process removal
            foreach (DownloadTask task in toRemove)
                Remove(task);

            System.Console.WriteLine("Added={0} Removed={1} Modified={2}", added, removed, modified);
        }

        private Dictionary<string, DownloadTask> TaskIndexing = new Dictionary<string,DownloadTask>();

        private DownloadTask FindTask(string id)
        {
            DownloadTask item;
            if (TaskIndexing.TryGetValue(id, out item))
            {
                return item;
            }
            return null;
        }

        private void CreateIndexing()
        {
            TaskIndexing.Clear();
            foreach (DownloadTask task in this)
                TaskIndexing[task.Id] = task;
        }
    }
}
