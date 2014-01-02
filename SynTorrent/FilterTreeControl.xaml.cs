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
    /// Interaction logic for FilterTreeControl.xaml
    /// </summary>
    public partial class FilterTreeControl : UserControl
    {
        public FilterTreeControl()
        {
            InitializeComponent();
            CreateDefaultFilters();

            // Set data source for the TreeView
            List<TaskFilterViewModel> source = new List<TaskFilterViewModel>();
            source.Add(FiltersTreeViewModel);
            FiltersTreeView.ItemsSource = source;

            SearchBox.TextBox.Text = Properties.Settings.Default.CurrentFilterKeywords;
        }

        public static readonly RoutedEvent SearchFieldEvent =
            EventManager.RegisterRoutedEvent(
                "SearchFieldEdited",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(FilterTreeControl));


        public event RoutedEventHandler SearchFieldEdited
        {
            add { AddHandler(SearchFieldEvent, value); }
            remove { RemoveHandler(SearchFieldEvent, value); }
        }

        private void CreateDefaultFilters()
        {
            RootTaskFilter filter = new RootTaskFilter();

            filter.AddFilter(new StatusTaskFilter("downloading"));
            filter.AddFilter(new UploadingTaskFilter());
            filter.AddFilter(new StatusTaskFilter("finished"));
            filter.AddFilter(new StatusTaskFilter("seeding"));
            filter.AddFilter(new StatusTaskFilter("error"));
            filter.AddFilter(new StatusTaskFilter("paused"));

            ActiveFilters.Add(SearchBoxTaskFilter);
            ActiveFilters.Add(filter);            

            AllTaskFilters = filter;

            RootFilter = new TaskFilterViewModel(AllTaskFilters);
            RootFilter.IsSelected = true;
        }

        public TaskFilterCollection ActiveFilters = new TaskFilterCollection();

        private RootTaskFilter AllTaskFilters;

        private FileNameTaskFilter SearchBoxTaskFilter = new FileNameTaskFilter();
        
        private TaskFilterViewModel RootFilter;

        public TaskFilterViewModel FiltersTreeViewModel
        {
            get { return RootFilter; }
        }
        
        private void RaiseSearchEvent()
        {
            RoutedEventArgs args = new RoutedEventArgs(SearchFieldEvent);
            RaiseEvent(args);
        }

        private void SearchBox_Search(object sender, RoutedEventArgs e)
        {
            // Update FileName filter with search keywords
            SearchBoxTaskFilter.QueryString = SearchBox.TextBox.Text;
            // Propagate event
            RaiseSearchEvent();
        }

        private void FiltersTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if(e.NewValue is TaskFilterViewModel)
            {
                TaskFilterViewModel taskViewModel = (TaskFilterViewModel)e.NewValue;

                // Rebuild list of active filters
                ActiveFilters.Clear();
                ActiveFilters.Add(SearchBoxTaskFilter);
                ActiveFilters.Add(taskViewModel.Filter);

                // Refresh list with new filters
                RaiseSearchEvent();
            }
        }
    }
}
