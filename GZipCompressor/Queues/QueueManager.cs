using GZipCompressor.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Queues
{
    public class QueueManager
    {
        private object sync = new object();
        private Queue<BytesBlock> queue { get; set; }
        //private ConcurrentQueue<BytesBlock> queue { get; set; }


        public QueueManager()
        {
            queue = new Queue<BytesBlock>();
        }

        //public QueueManager()
        //{
        //    queue = new ConcurrentQueue<BytesBlock>();
        //}

        public void Enqueue(byte[] bytes, int blockNumber)
        {
            var bytesBlock = new BytesBlock(bytes, blockNumber);

            lock (sync)
            {
                queue.Enqueue(bytesBlock);
            }
        }

        public BytesBlock Dequeue()
        {
            lock (sync)
            {
                if (queue.Count == 0)
                    return null;
                //BytesBlock value;
                //var success = queue.TryDequeue(out value);
                //if (success)
                //    return value;

                //return null;
                return queue.Dequeue();
            }
        }

        public bool IsEmpty()
        {
            lock (sync)
                return queue.Count == 0;
        }
    }
}
