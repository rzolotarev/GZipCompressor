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
    public class DecompressedFileWriter : FileWriter
    {
        public DecompressedFileWriter(string targetFilePath,
            DictionaryManager<int, byte[]> dictionaryWritingManager)
                                    : base(targetFilePath, dictionaryWritingManager)
        {

        }

        public override void Save()
        {
            byte[] buffer = null;
            try
            {
                using (var targetFileStream = File.Create(TargetFilePath))
                {
                    var orderNumber = 0;
                    while (!StatusManager.ProcessIsCanceled && StatusManager.Exception == null)
                    {
                        if (StatusManager.AllDecompressIsCompleted && DictionaryWritingManager.IsEmpty())
                        {
                            StatusManager.SavingToFileIsCompleted = true;
                            break;
                        }

                        var isSuccess = DictionaryWritingManager.TryRemove(orderNumber, out buffer);
                        if (!isSuccess)
                        {
                            Thread.Sleep(ThreadTimeout);
                            continue;
                        }

                        targetFileStream.Write(buffer, 0, buffer.Length);
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
