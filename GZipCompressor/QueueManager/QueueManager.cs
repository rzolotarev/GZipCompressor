using GZipCompressor.Contracts;
using GZipCompressor.Models;
using System.Collections.Generic;

namespace GZipCompressor.QueueManager
{
    public class QueueManager<T> : Queue<T>
    {
        private readonly object _locker = new object();        
        private readonly IThreadManager _threadManager;
        private readonly SyncBlock _syncBlock;
        
        public QueueManager(IThreadManager threadManager, SyncBlock syncBlock) : base()
        {            
            _threadManager = threadManager;
            _syncBlock = syncBlock;
        }

        public new void Enqueue(T value)
        {
            lock (_locker)
            {
                base.Enqueue(value);
                _threadManager.TryToWakeUp(_syncBlock);   
            }
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

        public void WaitOne()
        {
            _syncBlock.IsWorking = false;            
            _syncBlock.Event.WaitOne();
        }

        public void WakeUp()
        {
            _syncBlock.Event.Set();
        }
    }
}
