using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTMaster
{
    class QueueViewModel
    {
        public string Name
        {
            get;
            set;
        }

        public int? Length
        {
            get;
            set;
        }
    }

    class TTDataContext : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private IList<ActorAssignment> actors;
        private IList<QueueViewModel> queues;

        public IList<ActorAssignment> Actors
        {
            get
            {
                return this.actors;
            }
            set
            {
                this.actors = value;
                this.RaisePropertyChangedEvent("Actors");
            }
        }

        public IList<QueueViewModel> Queues
        {
            get
            {
                return this.queues;
            }
            set
            {
                this.queues = value;
                this.RaisePropertyChangedEvent("Queues");
            }
        }

        protected void RaisePropertyChangedEvent(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
