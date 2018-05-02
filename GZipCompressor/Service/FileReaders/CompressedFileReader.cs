using GZipCompressor.Models;
using GZipCompressor.Outputs;
using GZipCompressor.ProcessManagement;
using GZipCompressor.QueueManager;
using GZipCompressor.Service.FileReaders;
using Outputs;
using System;
using System.IO;

namespace GZipCompressor.Service
{
    public class CompressedFileReader : FileReader
    {        
        private const int SIZEBLOCK_LENGTH = 4;            

        public CompressedFileReader(string sourceFilePath, 
            QueueManager<BytesBlock>[] compressedDataManagers) : base(sourceFilePath, compressedDataManagers)
        {
          
        }            

        public override void Read()
        {
           using (var sourceFileStream = new FileStream(SourceFilePath, FileMode.Open))
                {
                    long fileSize = sourceFileStream.Length;
                    long currentPosition = 0;
                    var orderNumber = 0;
                    var queueNumber = 0;
                    var bufferForLength = new byte[SIZEBLOCK_LENGTH];

                    try
                    {
                        while ((sourceFileStream.Read(bufferForLength, 0, bufferForLength.Length) > 0)
                            && !StatusManager.ProcessIsCanceled && StatusManager.Exception == null)
                        {
                            byte[] buffer = new byte[BitConverter.ToInt32(bufferForLength, 0)];
                            sourceFileStream.Read(buffer, 0, buffer.Length);

                            queueNumber = queueNumber % ProcessorsCount;
                            CompressedDataManagers[queueNumber].Enqueue(new BytesBlock(buffer, orderNumber++));
                            queueNumber++;

                            currentPosition += buffer.Length + SIZEBLOCK_LENGTH;
                            ProgressBar.Print(currentPosition, fileSize, "Reading: Processed: ");
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
}
