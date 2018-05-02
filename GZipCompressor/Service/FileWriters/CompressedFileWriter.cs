using GZipCompressor.DictionaryManager;
using GZipCompressor.ProcessManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GZipCompressor.Service.FileWriters
{
    public class CompressedFileWriter : FileWriter
    {
        public CompressedFileWriter(string targetFilePath, 
            DictionaryManager<int, byte[]> dictionaryWritingManager) 
                                    : base(targetFilePath, dictionaryWritingManager)
        {

        }

        public override void Save()
        {
            var orderNumber = 0;
            byte[] bytesBlock = null;

            try
            {
                using (var targetFileStream = File.Create(TargetFilePath))
                {
                    while (!StatusManager.ProcessIsCanceled && StatusManager.Exception == null)
                    {
                        if (StatusManager.AllCompressIsCompleted && DictionaryWritingManager.IsEmpty())
                        {
                            StatusManager.SavingToFileIsCompleted = true;
                            break;
                        }

                        var isSuccess = DictionaryWritingManager.TryRemove(orderNumber, out bytesBlock);
                        if (!isSuccess)
                        {
                            Thread.Sleep(ThreadTimeout);
                            continue;
                        }

                        var blockLengthInBytes = BitConverter.GetBytes(bytesBlock.Length);
                        targetFileStream.Write(blockLengthInBytes, 0, blockLengthInBytes.Length);
                        targetFileStream.Write(bytesBlock, 0, bytesBlock.Length);

                        orderNumber++;
                    }
                }
            }
            catch (Exception ex)
            {
                StatusManager.Exception = ex;
            }
        }
    }
}
