using GZipCompressor.Models;
using GZipCompressor.Outputs;
using GZipCompressor.ProcessManagement;
using GZipCompressor.QueueManager;
using GZipCompressor.Service.FileReaders;
using Outputs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZipCompressor.Service.FileReaders
{
    public class UncompressedFileReader : FileReader
    {
        public UncompressedFileReader(string sourceFilePath,
            QueueManager<BytesBlock>[] compressedDataManagers) : base(sourceFilePath, compressedDataManagers)
        {

        }

        public override void Read()
        {
            long currentPosition = 0;
            var readedBytesCount = 0;
            var blockNumber = 0;
            var queueNumber = 0;

            byte[] buffer = new byte[BlockSizeToRead];

            try
            {
                using (var sourceFileStream = new FileStream(SourceFilePath, FileMode.Open, FileAccess.Read,
                                                        FileShare.Read, BlockSizeToRead))
                {
                    long fileSize = sourceFileStream.Length;

                    while (((readedBytesCount = sourceFileStream.Read(buffer, 0, buffer.Length)) > 0)
                        && !StatusManager.ProcessIsCanceled && StatusManager.Exception == null)
                    {
                        if (readedBytesCount < BlockSizeToRead)
                            Array.Resize(ref buffer, readedBytesCount);

                        queueNumber = queueNumber % ProcessorsCount;
                        CompressedDataManagers[queueNumber].Enqueue(new BytesBlock(buffer, blockNumber++));
                        queueNumber++;

                        currentPosition += readedBytesCount;
                        ProgressBar.Print(currentPosition, fileSize, "Reading: Processed: ");
                    }
                }
            }
            catch (OutOfMemoryException ex)
            {
                ConsoleLogger.WriteError($"Please make the size of chunk smaller... Current chunk size - {BlockSizeToRead} bytes");
                StatusManager.Exception = ex;
            }
            catch (Exception ex)
            {
                StatusManager.Exception = ex;
            }
            finally
            {
                if (!StatusManager.ProcessIsCanceled && StatusManager.Exception == null)
                {
                    Console.CursorLeft = 30;
                    Console.Write("Please, wait for ending of saving the file to disk...");
                    StatusManager.ReadingIsCompleted = true;
                }

                foreach (var dataManager in CompressedDataManagers)
                    dataManager.WakeUp();
            }
        }
    }
}
