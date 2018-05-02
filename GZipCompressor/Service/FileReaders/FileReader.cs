using GZipCompressor.ConfigurationManagers;
using GZipCompressor.Models;
using GZipCompressor.QueueManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZipCompressor.Service.FileReaders
{
    public abstract class FileReader
    {
        private const string CHUNK_SIZE = "ChunkSize";        

        protected readonly int BlockSizeToRead;
        protected readonly string SourceFilePath;
        protected readonly QueueManager<BytesBlock>[] CompressedDataManagers;
        protected int ProcessorsCount => Environment.ProcessorCount;

        public FileReader(string sourceFilePath, QueueManager<BytesBlock>[] compressedDataManagers)
        {
            BlockSizeToRead = SettingsManager.GetConfigParameter<int>(CHUNK_SIZE);
            SourceFilePath = sourceFilePath;
            CompressedDataManagers = compressedDataManagers;
        }

        public abstract void Read();
    }
}
