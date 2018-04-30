using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZipCompressor.QueueManager
{
    public class QueueManager<T> : Queue<T>
    {
        private readonly object _locker = new object();        

        public QueueManager() : base()
        {
        }

        public new void Enqueue(T value)
        {
            lock(_locker)
                base.Enqueue(value);
        }

        public bool IsEmpty()
        {
            lock (_locker)
                return base.Count == 0;
        }

        public bool TryDequeue(out T value)
        {
            value = default(T);

            lock (_locker)
            {
                if (base.Count == 0)
                    return false;

                value = base.Dequeue();
                return true;                
            }
        }
    }
}
