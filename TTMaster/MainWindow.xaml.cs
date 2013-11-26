using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;

namespace TTMaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ListSortDirection _lastDirection;
        private GridViewColumnHeader _lastHeaderClicked;
        private TTDataContext viewModel;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            viewModel = new TTDataContext() { };

            this.DataContext = viewModel;
        }


        private void ActorsItemContent_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.DevelopmentStorageAccount;

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            CloudTable table = tableClient.GetTableReference("topology");

            // Construct the query operation for all customer entities where PartitionKey="Smith".
            TableQuery<ActorAssignment> query = new TableQuery<ActorAssignment>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "Actor"));

            IList<ActorAssignment> actors = new List<ActorAssignment>();

            // Print the fields for each customer.
            foreach (ActorAssignment entity in table.ExecuteQuery(query))
            {
                actors.Add(entity);
            }

            this.viewModel.Actors = actors;
        }

        private void QueuesItemContent_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.DevelopmentStorageAccount;

            CloudQueueClient client = storageAccount.CreateCloudQueueClient();

            IList<QueueViewModel> queues = new List<QueueViewModel>();
            foreach (CloudQueue queue in client.ListQueues())
            {
                queue.FetchAttributes();
                queues.Add(new QueueViewModel() { Name = queue.Name, Length = queue.ApproximateMessageCount, });
            }

            this.viewModel.Queues = queues;
        }

        private void ActorsGridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            SortByClick(sender, e);
        }

        private void QueuesGridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            SortByClick(sender, e);
        }

        private void SortByClick(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    string header = headerClicked.Column.Header as string;
                    Sort(header, direction, sender as ListView);

                    if (direction == ListSortDirection.Ascending)
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowUp"] as DataTemplate;
                    }
                    else
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowDown"] as DataTemplate;
                    }

                    // Remove arrow from previously sorted header 
                    if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                    {
                        _lastHeaderClicked.Column.HeaderTemplate = null;
                    }


                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction, ListView listView)
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(listView.ItemsSource);

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }
    }
}
