using GZipCompressor.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZipCompressor.Archiever
{
    public class ThreadStarter
    {
        public bool LazyStartWasFired { get; private set; }
        public bool LazyStartAvailable { get; private set; }
        private long fileSize { get; set; }
        private int startPoint => 4;
        private int lazyFileSize => 200 * 1024 * 1024;

        public ThreadStarter()
        {

        }

        public ThreadStarter(long fileSize, int chunk, int threadsCount)
        {
            LazyStartAvailable = (fileSize > lazyFileSize) && ((fileSize / threadsCount * chunk) > startPoint);
        }

        public void StartLazyThread(SyncBlock syncBlock, int elementsCount)
        {
            if (elementsCount >= startPoint)
            {
                StartThread(syncBlock);
                LazyStartWasFired = true;
            }
        }

        public void StartThread(SyncBlock syncBlock)
        {
            if (!syncBlock.IsWorking)
            {
                syncBlock.IsWorking = true;
                syncBlock.Event.Set();
            }
        }

        public void StartThreads(SyncBlock[] syncBlocks)
        {
            foreach (var syncBlock in syncBlocks)
                StartThread(syncBlock);
        }
    }
}
