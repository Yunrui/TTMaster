using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace TTMaster
{
    /// <summary>
    /// ActorEntity
    /// </summary>
    public class ActorAssignment : TableEntity
    {
        /// <summary>
        /// Suppose Topology Table is pretty small, so it's not necessary to Partition
        /// </summary>
        public const string Key = "Actor";

        public ActorAssignment(Guid actorId)
        {
            this.PartitionKey = ActorAssignment.Key;
            this.RowKey = actorId.ToString();
        }

        public ActorAssignment() { }

        public string Name { get; set; }

        public string Topology { get; set; }

        public string InQueue { get; set; }

        public string OutQueues { get; set; }

        public string SchemaGroupingMode { get; set; }

        public string GroupingField { get; set; }

        public bool? IsSpout { get; set; }

        public string State { get; set; }

        public string Operation { get; set; }

        public DateTime HeartBeat { get; set; }
    }
}
