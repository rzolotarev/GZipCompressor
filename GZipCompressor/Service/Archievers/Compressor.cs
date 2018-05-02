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

namespace GZipCompressor.Service.Archievers
{
    public class Compressor : Archiever
    {
        public Compressor(QueueManager<BytesBlock>[] compressedDataManagers,
                         DictionaryManager<int, byte[]> dictionaryWritingManager) 
            : base(compressedDataManagers, dictionaryWritingManager)
        {

        }

        public override void Start(int threadNumber)
        {
            var threadCompressedDataManager = CompressedDataManagers[threadNumber];
            threadCompressedDataManager.WaitOne();
            Interlocked.Increment(ref UnsyncThreads);
            BytesBlock bytesBlock = null;

            try
            {
                while (!StatusManager.ProcessIsCanceled && StatusManager.Exception == null)
                {
                    if (StatusManager.ReadingIsCompleted && threadCompressedDataManager.IsEmpty())
                    {
                        Interlocked.Decrement(ref UnsyncThreads);
                        if (UnsyncThreads == 0) StatusManager.AllCompressIsCompleted = true;
                        break;
                    }

                    var isSuccess = threadCompressedDataManager.TryDequeue(out bytesBlock);
                    if (!isSuccess)
                    {
                        threadCompressedDataManager.WaitOne();
                        continue;
                    }

                    var buffer = BytesCompressUtil.CompressBytes(bytesBlock.Buffer);
                    DictionaryWritingManager.Add(bytesBlock.OrderNumber, buffer);
                }
            }
            catch (OutOfMemoryException ex)
            {
                GC.Collect();
                threadCompressedDataManager.Enqueue(new BytesBlock(bytesBlock.Buffer, bytesBlock.OrderNumber));
            }
        }
    }
}
