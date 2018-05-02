using GZipCompressor.ConfigurationManagers;
using GZipCompressor.DictionaryManager;
using GZipCompressor.Models;
using GZipCompressor.QueueManager;
using System;
using System.Collections.Concurrent;

namespace GZipCompressor.Archiever
{
    public abstract class GZipBlockArchiver
    {
        public static bool ProcessIsCanceled { get; set; } = false;

        protected readonly IThreadManager _threadManager;

        private static string chunkSize => "ChunkSize";
        private static string threadTimeout => "ThreadTimeout";
        protected int CoresCount => Environment.ProcessorCount;
        protected readonly int BlockSizeToRead;
       
        protected string SourceFilePath { get; private set; }
        protected string TargetFilePath { get; private set; }        
        protected QueueManager<BytesBlock>[] CompressedDataManagers { get; set; }
        protected DictionaryManager<int, byte[]> DictionaryWritingManager { get; set; }        
        protected static bool ReadingIsCompleted { get; set; }
        protected static bool SavingToFileIsCompleted { get; set; }
        protected static int UnsyncThreads = 0;
        protected int ThreadTimeout { get; set; }
        protected Exception exception { get; set; }

        public GZipBlockArchiver(string sourceFilePath, string targetFilePath, 
                                 IThreadManager threadManager, long fileSize)
        {
            SourceFilePath = sourceFilePath;
            TargetFilePath = targetFilePath;
            _threadManager = threadManager;
            BlockSizeToRead = SettingsManager.GetConfigParameter<int>(chunkSize);
            ThreadTimeout = SettingsManager.GetConfigParameter<int>(threadTimeout);                               
            DictionaryWritingManager = new DictionaryManager<int, byte[]>((int)(fileSize / BlockSizeToRead) + 1);         
            CompressedDataManagers = new QueueManager<BytesBlock>[CoresCount];
            InitQueueManagers(CompressedDataManagers);
        }

        public void SetCancelStatus(bool status)
        {
            ProcessIsCanceled = status;
        }

        public bool GetCancelStatus()
        {
            return ProcessIsCanceled;
        }

        private void InitDefaultQueues<T>(T[] queue) where T : class, new()
        {
            for (int i = 0; i < queue.Length; i++)
                queue[i] = new T();
        }

        private void InitQueueManagers(QueueManager<BytesBlock>[] queuesManager)
        {
            var syncs = new SyncBlock[CoresCount];
            InitDefaultQueues(syncs);

            for (int i = 0; i < queuesManager.Length; i++)
                queuesManager[i] = new QueueManager<BytesBlock>(_threadManager, syncs[i]);
        }

        public abstract bool Start();
    }
}
