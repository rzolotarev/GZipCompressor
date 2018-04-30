using GZipCompressor;
using GZipCompressor.Archiever;
using GZipCompressor.Models;
using GZipCompressor.Outputs;
using GZipCompressor.Service;
using Outputs;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace Services.Decompressor
{
    public class Decompressor : GZipBlockArchiver
    {
        private const int sizeBlockLength = 4;             
        
        private bool allDecompressIsCompleted { get; set; } = false;


        public Decompressor(string sourceFilePath, string targetFilePath, long fileSize)
                                 : base(sourceFilePath, targetFilePath, fileSize)
        {            
            
        }        

        public override bool Start()
        {
            ConsoleLogger.WriteDiagnosticInfo($"Decompressing of {SourceFilePath} to {TargetFilePath} is started...");

            try
            {
                new Thread(ReadSourceFile).Start();

                for (int i = 0; i < CoresCount; i++)
                {
                    var currentThread = i;
                    new Thread(() => Decompress(currentThread)).Start();
                }

                var threadToWrite = new Thread(WriteToTargetFile);
                threadToWrite.Start();

                threadToWrite.Join();
                return true;
            }
            catch (Exception ex)
            {
                ConsoleLogger.WriteError(ex.Message);
                return false;
            }
        }

        public void ReadSourceFile()
        {
            using (var sourceFileStream = new FileStream(SourceFilePath, FileMode.Open))
            {
                long fileSize = sourceFileStream.Length;
                long currentPosition = 0;
                var orderNumber = 0;
                var queueNumber = 0;
                var bufferForLength = new byte[sizeBlockLength];

                var threadsRunner = new ThreadStarter();

                try
                {
                    while ((sourceFileStream.Read(bufferForLength, 0, bufferForLength.Length) > 0) && !ProcessIsCanceled)
                    {
                        byte[] buffer = new byte[BitConverter.ToInt32(bufferForLength, 0)];
                        sourceFileStream.Read(buffer, 0, buffer.Length);

                        queueNumber = queueNumber % CoresCount;

                        CompressedDataQueues[queueNumber].Enqueue(new BytesBlock(buffer, orderNumber++));

                        threadsRunner.StartThread(Syncs[queueNumber]);

                        currentPosition += buffer.Length + sizeBlockLength;
                        queueNumber++;
                        ProgressBar.Print(currentPosition, fileSize, "Reading: Processed: ");
                    }
                }
                catch (OutOfMemoryException ex)
                {
                    ConsoleLogger.WriteError($"Please make the size of chunk smaller... Current chunk size - {BlockSizeToRead} bytes");
                    throw ex;
                }            

                if (!ProcessIsCanceled)
                {
                    Console.CursorLeft = 30;
                    Console.Write("Please, wait for ending of saving the file to disk...");
                    ReadingIsCompleted = true;                    
                }

                for (int i = 0; i < CoresCount; i++)
                    Syncs[i].Event.Set();
            }
        }

        private void Decompress(int threadNumber)
        {
            Syncs[threadNumber].Event.WaitOne();
            Interlocked.Increment(ref UnsyncThreads);

            BytesBlock bytesBlock = null;

            try
            {
                while (!ProcessIsCanceled)
                {
                    if (ReadingIsCompleted && CompressedDataQueues[threadNumber].IsEmpty())
                    {
                        Interlocked.Decrement(ref UnsyncThreads);
                        if (UnsyncThreads == 0) allDecompressIsCompleted = true;
                        break;
                    }

                    var isSuccess = CompressedDataQueues[threadNumber].TryDequeue(out bytesBlock);
                    if (!isSuccess)
                    {
                        Syncs[threadNumber].IsWorking = false;
                        Syncs[threadNumber].Event.WaitOne();
                        continue;
                    }

                    var buffer = BytesCompressUtil.DecompressBytes(bytesBlock.Buffer, BlockSizeToRead);

                    DictionaryToWrite.Add(bytesBlock.OrderNumber, buffer);
                }
            }
            catch (OutOfMemoryException ex)
            {
                GC.Collect();
                CompressedDataQueues[threadNumber].Enqueue(new BytesBlock(bytesBlock.Buffer, bytesBlock.OrderNumber));
            }
        }        

        public void WriteToTargetFile()
        {
            byte[] buffer = null;

            using (var targetFileStream = File.Create(TargetFilePath))
            {
                var orderNumber = 0;
                while (!ProcessIsCanceled)
                {                 
                    if (allDecompressIsCompleted && DictionaryToWrite.IsEmpty())
                    {
                        SavingToFileIsCompleted = true;                     
                        break;
                    }
                   
                    var isSuccess = DictionaryToWrite.TryRemove(orderNumber, out buffer);
                    if(!isSuccess)
                    {                       
                        Thread.Sleep(ThreadTimeout);
                        continue;
                    }                    

                    targetFileStream.Write(buffer, 0, buffer.Length);
                    orderNumber++;                                       
                }
            }
        }
    }
}
