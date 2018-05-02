using GZipCompressor.ConfigurationManagers;
using GZipCompressor.Contracts;
using GZipCompressor.DictionaryManager;
using GZipCompressor.Models;
using GZipCompressor.QueueManager;
using System;
using System.Collections.Concurrent;

namespace GZipCompressor.GZipArchiever
{
    public abstract class GZipBlockArchiver
    {
        private const string CHUNK_SIZE = "ChunkSize";        
        protected readonly IThreadManager _threadManager;        
        protected int CoresCount => Environment.ProcessorCount;               
        protected string SourceFilePath { get; private set; }
        protected string TargetFilePath { get; private set; }        
        public QueueManager<BytesBlock>[] CompressedDataManagers { get; private set; }
        public DictionaryManager<int, byte[]> DictionaryWritingManager { get; private set; }

        public GZipBlockArchiver(string sourceFilePath, string targetFilePath, 
                                 IThreadManager threadManager, long fileSize)
        {
            SourceFilePath = sourceFilePath;
            TargetFilePath = targetFilePath;
            _threadManager = threadManager;
            var blockSizeToRead = SettingsManager.GetConfigParameter<int>(CHUNK_SIZE);
            DictionaryWritingManager = new DictionaryManager<int, byte[]>((int)(fileSize / blockSizeToRead) + 1);         
            CompressedDataManagers = new QueueManager<BytesBlock>[CoresCount];
            InitQueueManagers(CompressedDataManagers);
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

        public abstract bool Start(IFileReader fileReader, IArchiever archiever, IFileWriter fileWriter);
    }
}
