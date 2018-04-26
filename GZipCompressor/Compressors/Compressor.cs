using GZipCompressor;
using GZipCompressor.Archiever;
using GZipCompressor.Models;
using GZipCompressor.Outputs;
using GZipCompressor.Service;
using Outputs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace Compressors
{
    public class Compressor : GZipBlockArchiver
    {                       
        private static bool allCompressIsCompleted { get; set; } = false;       

        public Compressor(string sourceFilePath, string targetFilePath, long fileSize)
                                     : base(sourceFilePath, targetFilePath, fileSize)
        {                                                        
        }             

        public override bool Start()
        {
            ConsoleLogger.WriteDiagnosticInfo($"Compressing of {SourceFilePath} to {TargetFilePath} is started...");
            
            try
            {    
                new Thread(ReadSourceFile).Start();

                var compressThreads = new Thread[CoresCount];
                for (int i = 0; i < CoresCount; i++)
                {
                    var current = i;
                    new Thread(() => Compress(current)).Start();                    
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
            long currentPosition = 0;
            var readedBytesCount = 0;
            var blockNumber = 0;
            var queueNumber = 0;

            byte[] buffer = new byte[BlockSizeToRead];

            using (var sourceFileStream = new FileStream(SourceFilePath, FileMode.Open, FileAccess.Read,
                                                        FileShare.Read, BlockSizeToRead))
            {
                long fileSize = sourceFileStream.Length;

                var threadsRunner = new ThreadStarter();
                try
                {
                    while (((readedBytesCount = sourceFileStream.Read(buffer, 0, buffer.Length)) > 0)
                        && !ProcessIsCanceled)
                    {
                        if (readedBytesCount < BlockSizeToRead)
                            Array.Resize(ref buffer, readedBytesCount);

                        queueNumber = queueNumber % CoresCount;

                        CompressedDataQueues[queueNumber].Enqueue(new BytesBlock(buffer, blockNumber++));

                        threadsRunner.StartThread(Syncs[queueNumber]);

                        currentPosition += readedBytesCount;
                        queueNumber++;
                        ProgressBar.Print(currentPosition, fileSize, "Reading: Processed: ");
                    }
                }
                catch (OutOfMemoryException ex)
                {
                    ConsoleLogger.WriteError($"Please make the size of chunk smaller... Current chunk size - {BlockSizeToRead} bytes");
                    throw ex;
                }
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

        public void Compress(int threadNumber)
        {           
            Syncs[threadNumber].Event.WaitOne();
            Interlocked.Increment(ref UnsyncThreads);
            BytesBlock bytesBlock = null;

            try
            {
                while (!ProcessIsCanceled)
                {              
                    if (ReadingIsCompleted && CompressedDataQueues[threadNumber].IsEmpty)
                    {                                           
                        Interlocked.Decrement(ref UnsyncThreads);                        
                        if (UnsyncThreads == 0) allCompressIsCompleted = true;
                        break;
                    }

                    var isSuccess = CompressedDataQueues[threadNumber].TryDequeue(out bytesBlock);
                    if (!isSuccess)
                    {    
                        Syncs[threadNumber].IsWorking = false;
                        Syncs[threadNumber].Event.WaitOne();
                        continue;
                    }

                    var buffer = BytesCompressUtil.CompressBytes(bytesBlock.Buffer);
                    DictionaryToWrite.TryAdd(bytesBlock.OrderNumber, buffer);               
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
            var orderNumber = 0;

            byte[] bytesBlock = null;
            using (var targetFileStream = File.Create(TargetFilePath))
            {                
                while (!ProcessIsCanceled)
                {                    
                    if (allCompressIsCompleted && DictionaryToWrite.IsEmpty)
                    {
                        SavingToFileIsCompleted = true;                        
                        break;
                    }
                    
                    var isSuccess = DictionaryToWrite.TryRemove(orderNumber, out bytesBlock);
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
    }
}
