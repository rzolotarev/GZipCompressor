using GZipCompressor.ConfigurationManagers;
using GZipCompressor.DictionaryManager;
using GZipCompressor.Extensions;
using GZipCompressor.Models;
using GZipCompressor.ProcessManagement;
using GZipCompressor.QueueManager;
using GZipCompressor.Service;
using GZipCompressor.Service.Archievers;
using GZipCompressor.Service.FileReaders;
using Outputs;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace GZipCompressor.GZipArchiever
{
    public class GZipBlockArchiver
    {
        private const string CHUNK_SIZE = "ChunkSize";        
        private readonly ThreadManager _threadManager;    
        
        private int coresCount => Environment.ProcessorCount;               
        private string _sourceFilePath { get; set; }
        private string _targetFilePath { get; set; }        
        public QueueManager<BytesBlock>[] CompressedDataManagers { get; private set; }
        public DictionaryManager<int, byte[]> DictionaryWritingManager { get; private set; }

        public GZipBlockArchiver(string sourceFilePath, string targetFilePath, 
                                 ThreadManager threadManager, long fileSize)
        {
            _sourceFilePath = sourceFilePath;
            _targetFilePath = targetFilePath;
            _threadManager = threadManager;
            var blockSizeToRead = SettingsManager.GetConfigParameter<int>(CHUNK_SIZE);
            DictionaryWritingManager = new DictionaryManager<int, byte[]>((int)(fileSize / blockSizeToRead) + 1);         
            CompressedDataManagers = new QueueManager<BytesBlock>[coresCount];
            InitQueueManagers(CompressedDataManagers);
        }       

        private void InitDefaultQueues<T>(T[] queue) where T : class, new()
        {
            for (int i = 0; i < queue.Length; i++)
                queue[i] = new T();
        }

        private void InitQueueManagers(QueueManager<BytesBlock>[] queuesManager)
        {
            var syncs = new SyncBlock[coresCount];
            InitDefaultQueues(syncs);

            for (int i = 0; i < queuesManager.Length; i++)
                queuesManager[i] = new QueueManager<BytesBlock>(_threadManager, syncs[i]);
        }

        public bool Start(FileReader fileReader, Archiever archiever, FileWriter fileWriter, OperationType operationType)
        {
            ConsoleLogger.WriteDiagnosticInfo($"{operationType.DisplayName()} of {_sourceFilePath} to {_targetFilePath} is started...");

            new Thread(fileReader.Read).Start();            

            for (int i = 0; i < coresCount; i++)
            {
                var current = i;
                new Thread(() => archiever.Start(current)).Start();
            }

            var threadToWrite = new Thread(fileWriter.Save);
            threadToWrite.Start();

            threadToWrite.Join();
            if (StatusManager.Exception != null)
            {
                ConsoleLogger.WriteError(StatusManager.Exception.Message);
                return false;
            }

            return true;
        }
    }
}
