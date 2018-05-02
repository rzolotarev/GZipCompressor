using GZipCompressor.Contracts;
using GZipCompressor.DictionaryManager;
using GZipCompressor.Models;
using GZipCompressor.ProcessManagement;
using GZipCompressor.QueueManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GZipCompressor.Service
{
    public class Archiever : IArchiever
    {
        private static int unsyncThreads;
        private readonly QueueManager<BytesBlock>[] _compressedDataManagers;
        private readonly DictionaryManager<int, byte[]> _dictionaryWritingManager;        

        public Archiever(QueueManager<BytesBlock>[] compressedDataManagers,
                         DictionaryManager<int, byte[]> dictionaryWritingManager)
        {
            _compressedDataManagers = compressedDataManagers;
            _dictionaryWritingManager = dictionaryWritingManager;
        }

        public void Compress(int threadNumber)
        {
            var threadCompressedDataManager = _compressedDataManagers[threadNumber];
            threadCompressedDataManager.WaitOne();
            Interlocked.Increment(ref unsyncThreads);
            BytesBlock bytesBlock = null;

            try
            {
                while (!StatusManager.ProcessIsCanceled && StatusManager.Exception == null)
                {
                    if (StatusManager.ReadingIsCompleted && threadCompressedDataManager.IsEmpty())
                    {
                        Interlocked.Decrement(ref unsyncThreads);
                        if (unsyncThreads == 0) StatusManager.AllCompressIsCompleted = true;
                        break;
                    }

                    var isSuccess = threadCompressedDataManager.TryDequeue(out bytesBlock);
                    if (!isSuccess)
                    {
                        threadCompressedDataManager.WaitOne();
                        continue;
                    }

                    var buffer = BytesCompressUtil.CompressBytes(bytesBlock.Buffer);
                    _dictionaryWritingManager.Add(bytesBlock.OrderNumber, buffer);
                }
            }
            catch (OutOfMemoryException ex)
            {
                GC.Collect();
                threadCompressedDataManager.Enqueue(new BytesBlock(bytesBlock.Buffer, bytesBlock.OrderNumber));
            }
        }

        public void Decompress(int threadNumber)
        {
            var threadCompressedDataManager = _compressedDataManagers[threadNumber];
            threadCompressedDataManager.WaitOne();

            Interlocked.Increment(ref unsyncThreads);
            BytesBlock bytesBlock = null;

            while (!StatusManager.ProcessIsCanceled && StatusManager.Exception == null)
            {
                if (StatusManager.ReadingIsCompleted && threadCompressedDataManager.IsEmpty())
                {
                    Interlocked.Decrement(ref unsyncThreads);
                    if (unsyncThreads == 0) StatusManager.AllDecompressIsCompleted = true;
                    break;
                }

                var isSuccess = threadCompressedDataManager.TryDequeue(out bytesBlock);
                if (!isSuccess)
                {
                    threadCompressedDataManager.WaitOne();
                    continue;
                }

                var buffer = BytesCompressUtil.DecompressBytes(bytesBlock.Buffer);

                _dictionaryWritingManager.Add(bytesBlock.OrderNumber, buffer);
            }
        }
    }
}
