using GZipCompressor.ConfigurationManagers;
using GZipCompressor.Models;
using System;
using System.Collections.Concurrent;

namespace GZipCompressor.Archiever
{
    public abstract class GZipBlockArchiver
    {
        public static bool ProcessIsCanceled { get; set; } = false;

        private static string chunkSize => "ChunkSize";
        private static string threadTimeout => "ThreadTimeout";        
        
        protected readonly int BlockSizeToRead;
       
        protected string SourceFilePath { get; private set; }
        protected string TargetFilePath { get; private set; }
        protected int CoresCount { get; private set; }
        protected ConcurrentQueue<BytesBlock>[] CompressedDataQueues { get; set; }
        protected ConcurrentDictionary<int, byte[]> DictionaryToWrite { get; set; }
        protected SyncBlock[] Syncs { get; set; }
        protected static bool ProcessIsCompleted { get; set; } = false;        
        protected static bool ReadingIsCompleted { get; set; } = false;
        protected static bool SavingToFileIsCompleted { get; set; } = false;
        protected static int UnsyncThreads = 0;
        protected int ThreadTimeout { get; set; }

        public GZipBlockArchiver(string sourceFilePath, string targetFilePath, 
                                 long fileSize)
        {
            SourceFilePath = sourceFilePath;
            TargetFilePath = targetFilePath;
            CoresCount = Environment.ProcessorCount;
            BlockSizeToRead = SettingsManager.GetConfigParameter<int>(chunkSize);
            ThreadTimeout = SettingsManager.GetConfigParameter<int>(threadTimeout);
            CompressedDataQueues = new ConcurrentQueue<BytesBlock>[CoresCount];
            InitQueues(CompressedDataQueues);          
            DictionaryToWrite = new ConcurrentDictionary<int, byte[]>(CoresCount,(int)(fileSize / BlockSizeToRead) + 1);
            Syncs = new SyncBlock[CoresCount];
            InitQueues(Syncs);
        }

        public void SetCancelStatus(bool status)
        {
            ProcessIsCanceled = status;
        }

        public bool GetCancelStatus()
        {
            return ProcessIsCanceled;
        }

        protected void InitQueues<T>(T[] queue) where T : class, new()
        {
            for (int i = 0; i < queue.Length; i++)
                queue[i] = new T();
        }

        public abstract bool Start();
    }
}
