using GZipCompressor.Models;

namespace GZipCompressor.GZipArchiever
{
    public class ThreadManager
    {
        public bool LazyStartWasFired { get; private set; }
        public bool LazyStartAvailable { get; private set; }
        private long fileSize { get; set; }
        private int startPoint => 4;
        private int lazyFileSize => 200 * 1024 * 1024;

        public ThreadManager()
        {

        }

        public ThreadManager(long fileSize, int chunk, int threadsCount)
        {
            LazyStartAvailable = (fileSize > lazyFileSize) && ((fileSize / threadsCount * chunk) > startPoint);
        }

        public void StartLazyThread(SyncBlock syncBlock, int elementsCount)
        {
            if (elementsCount >= startPoint)
            {
                TryToWakeUp(syncBlock);
                LazyStartWasFired = true;
            }
        }

        public void TryToWakeUp(SyncBlock syncBlock)
        {
            if (!syncBlock.IsWorking)
            {
                syncBlock.IsWorking = true;
                syncBlock.Event.Set();
            }
        }       
    }
}
