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
using System.Net;
using System.Diagnostics;
using Microsoft.WindowsAzure.Storage;

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

        private static List<ActorAssignment> cfrAssignments = new List<ActorAssignment>()
        {
            new ActorAssignment(Guid.Empty)
                    {
                        Topology = "CFRTopology",
                        Name = "TagIdSpout",
                        IsSpout = true,
                        InQueue = string.Empty,
                        OutQueues = "cfroutput1,cfroutput2",
                        SchemaGroupingMode = "FieldGrouping",
                        GroupingField = "tagId,dateTime",
                        HeartBeat = DateTime.UtcNow,
                    },

            new ActorAssignment(Guid.Empty)
                    {
                        Topology = "CFRTopology",
                        Name = "TagIdGroupBolt",
                        IsSpout = false,
                        InQueue = "cfroutput1",
                        OutQueues = "cfroutput3,cfroutput4",
                        SchemaGroupingMode = "FieldGrouping",
                        GroupingField = "page,dateTime",
                        HeartBeat = DateTime.UtcNow,
                    },

            new ActorAssignment(Guid.Empty)
                    {
                        Topology = "CFRTopology",
                        Name = "TagIdGroupBolt",
                        IsSpout = false,
                        InQueue = "cfroutput2",
                        OutQueues = "cfroutput3,cfroutput4",
                        SchemaGroupingMode = "FieldGrouping",
                        GroupingField = "page,dateTime",
                        HeartBeat = DateTime.UtcNow,
                    },

            new ActorAssignment(Guid.Empty)
                    {
                        Topology = "CFRTopology",
                        Name = "PageGroupBolt",
                        IsSpout = false,
                        InQueue = "cfroutput3",
                        HeartBeat = DateTime.UtcNow,
                    },

            new ActorAssignment(Guid.Empty)
                    {
                        Topology = "CFRTopology",
                        Name = "PageGroupBolt",
                        IsSpout = false,
                        InQueue = "cfroutput4",
                        HeartBeat = DateTime.UtcNow,
                    },
        };


        public MainWindow()
        {
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;
            ServicePointManager.Expect100Continue = false;

            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            viewModel = new TTDataContext() { };

            this.DataContext = viewModel;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.actorsItemContent.IsSelected)
            {
                LoadActors();
            }
            else
            {
                LoadQueues();
            }
        }

        private void CFRButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.actorsItemContent.IsSelected)
            {
                if (this.TopologyListView.SelectedItems.Count != 5)
                {
                    MessageBox.Show("Five items must be selected to clone.");
                    return;
                }

                foreach (ActorAssignment assignment in this.TopologyListView.SelectedItems)
                {
                    if (assignment.State != "NewBorn")
                    {
                        MessageBox.Show("All Items must be in NewBorn state.");
                        return;
                    }
                }

                Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = GetStorageAccount();
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable table = tableClient.GetTableReference("topology");

                for (int i = 0; i < 5; i++)
                {
                    cfrAssignments[i].RowKey = ((ActorAssignment)this.TopologyListView.SelectedItems[i]).RowKey;
                    cfrAssignments[i].ETag = "*";

                    TableOperation mergeOperation = TableOperation.Merge(cfrAssignments[i]);
                    TableResult retrievedResult = table.Execute(mergeOperation);
                }
            }
        }

        private void CloneButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.actorsItemContent.IsSelected)
            {
                if (this.TopologyListView.SelectedItems.Count != 2)
                {
                    MessageBox.Show("Two items must be selected to clone.");
                    return;
                }

                ActorAssignment source = null;
                ActorAssignment dest = null;

                ActorAssignment first = this.TopologyListView.SelectedItems[0] as ActorAssignment;
                ActorAssignment second = this.TopologyListView.SelectedItems[1] as ActorAssignment;

                if (first.State != "NewBorn" && second.State != "NewBorn")
                {
                    MessageBox.Show("One of the selection must be a NewBorn.");
                    return;
                }
                else if (first.State == "NewBorn" && second.State == "NewBorn")
                {
                    MessageBox.Show("Only one NewBorn is allowed.");
                    return;
                }
                else
                {
                    source = first.State == "NewBorn" ? second : first;
                    dest = first.State == "NewBorn" ? first : second;

                    Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = GetStorageAccount();
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                    CloudTable table = tableClient.GetTableReference("topology");

                    dest.InQueue = source.InQueue;
                    dest.IsSpout = source.IsSpout;
                    dest.Name = source.Name;
                    dest.OutQueues = source.OutQueues;
                    dest.SchemaGroupingMode = source.SchemaGroupingMode;
                    dest.Topology = source.Topology;
                    dest.GroupingField = source.GroupingField;
                    dest.ETag = "*";

                    TableOperation mergeOperation = TableOperation.Merge(dest);
                    TableResult retrievedResult = table.Execute(mergeOperation);
                }
            }
        }

        private void KillButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.actorsItemContent.IsSelected)
            {
                var index = this.TopologyListView.SelectedIndex;

                if (this.TopologyListView.SelectedValue != null)
                {
                    Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = GetStorageAccount();

                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

                    CloudTable table = tableClient.GetTableReference("topology");

                    var assignment = this.TopologyListView.SelectedValue as ActorAssignment;
                    assignment.Operation = "Kill";
                    assignment.ETag = "*";
                    TableOperation mergeOperation = TableOperation.Merge(assignment);
                    TableResult retrievedResult = table.Execute(mergeOperation);
                }
            }
        }

        private void CFRQueueButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse("DefaultEndpointsProtocol=http;AccountName=lqueue;AccountKey=vtVWSPvFXzJ3WzHcKpFbU9GY5YNsDGs493FMaxXpZFhwLN/pyfICpAOQcfj+QSP8T/r4yeIEHLOgKurPPB9EPQ==");
            CloudQueueClient client = storageAccount.CreateCloudQueueClient();
            var queue = client.GetQueueReference("cfr");

            queue.FetchAttributes();

            MessageBox.Show(string.Format("CFR queue has around {0} items.", queue.ApproximateMessageCount));
        }

        private void ActorsItemContent_Loaded(object sender, RoutedEventArgs e)
        {
            LoadActors();
        }

        private void LoadActors()
        {
            Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = GetStorageAccount();

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
            LoadQueues();
        }

        private void LoadQueues()
        {
            Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = GetStorageAccount();

            CloudQueueClient client = storageAccount.CreateCloudQueueClient();

            IList<QueueViewModel> queues = new List<QueueViewModel>();
            foreach (CloudQueue queue in client.ListQueues())
            {
                queue.FetchAttributes();
                queues.Add(new QueueViewModel() { Name = queue.Name, Length = queue.ApproximateMessageCount, });
            }

            this.viewModel.Queues = queues;
        }

        private Microsoft.WindowsAzure.Storage.CloudStorageAccount GetStorageAccount(bool devboxForce = false)
        {
            if (devboxForce || this.envCheckBox.IsChecked.HasValue && this.envCheckBox.IsChecked.Value)
            {
                return Microsoft.WindowsAzure.Storage.CloudStorageAccount.DevelopmentStorageAccount;
            }
            else
            {
                return Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=taotie;AccountKey=cA2QpkcF0i5JJ2tXVlFrPOsw+cDxj/NsNq3x00tZ7HLCicUewgcXxY0Q1gY34A+sgNcXa8BJdr8ONZMnfwKfnA==");
            }
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

        CloudQueue asyncQueue = null;
        private void PerfButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = GetStorageAccount();
            CloudQueueClient client = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = client.GetQueueReference("perf");
            queue.DeleteIfExists();
            queue.CreateIfNotExists();

            CloudQueue queueAsync = client.GetQueueReference("async");
            queueAsync.DeleteIfExists();
            queueAsync.CreateIfNotExists();
            this.asyncQueue = queueAsync;

            int count = 0;

            Stopwatch watch = new Stopwatch();
            watch.Start();

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += worker_DoWork;
            worker.RunWorkerAsync();

            do
            {
                var message = new CloudQueueMessage("Hello Queue");
                var results = queueAsync.GetMessages(32);

                count += results.Count();

                if (count % 100 == 0)
                {
                    Debug.WriteLine(string.Format("It takes {0} seconds to load {1} objects", watch.ElapsedMilliseconds / 1000, count));
                }
            }
            while (true);
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            do
            {
                var message = new CloudQueueMessage("Hello Queue");
                this.asyncQueue.AddMessageAsync(message);
            }
            while (true);
        }
    }
}
