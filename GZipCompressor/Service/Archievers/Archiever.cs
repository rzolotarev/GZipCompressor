using GZipCompressor.DictionaryManager;
using GZipCompressor.Models;
using GZipCompressor.QueueManager;

namespace GZipCompressor.Service.Archievers
{
    public abstract class Archiever
    {
        protected static int UnsyncThreads;
        private QueueManager<BytesBlock>[] compressedDataManagers;
        protected readonly QueueManager<BytesBlock>[] CompressedDataManagers;
        protected readonly DictionaryManager<int, byte[]> DictionaryWritingManager;        

        public Archiever(QueueManager<BytesBlock>[] compressedDataManagers,
                         DictionaryManager<int, byte[]> dictionaryWritingManager)
        {
            CompressedDataManagers = compressedDataManagers;
            DictionaryWritingManager = dictionaryWritingManager;
        }      

        public abstract void Start(int threadNumber);      
    }
}
