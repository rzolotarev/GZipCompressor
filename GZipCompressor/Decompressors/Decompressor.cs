﻿using GZipCompressor;
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

        public override bool Start(IThreadManager threadManager)
        {
            ConsoleLogger.WriteDiagnosticInfo($"Decompressing of {SourceFilePath} to {TargetFilePath} is started...");

            try
            {
                new Thread(() => ReadSourceFile(threadManager)).Start();

                for (int i = 0; i < CoresCount; i++)
                {
                    var currentThread = i;
                    new Thread(() => Decompress(currentThread)).Start();
                }

                var threadToWrite = new Thread(WriteToTargetFile);
                threadToWrite.Start();

                threadToWrite.Join();
                if (exception != null)
                    throw exception;

                return true;
            }
            catch (Exception ex)
            {
                ConsoleLogger.WriteError(ex.Message);
                return false;
            }
        }

        public void ReadSourceFile(IThreadManager threadManager)
        {
            using (var sourceFileStream = new FileStream(SourceFilePath, FileMode.Open))
            {
                long fileSize = sourceFileStream.Length;
                long currentPosition = 0;
                var orderNumber = 0;
                var queueNumber = 0;
                var bufferForLength = new byte[sizeBlockLength];

                try
                {
                    while ((sourceFileStream.Read(bufferForLength, 0, bufferForLength.Length) > 0)
                        && !ProcessIsCanceled && exception == null)
                    {
                        byte[] buffer = new byte[BitConverter.ToInt32(bufferForLength, 0)];
                        sourceFileStream.Read(buffer, 0, buffer.Length);

                        queueNumber = queueNumber % CoresCount;

                        CompressedDataQueues[queueNumber].Enqueue(new BytesBlock(buffer, orderNumber++));

                        threadManager.TryToWakeUp(Syncs[queueNumber]);

                        currentPosition += buffer.Length + sizeBlockLength;
                        queueNumber++;
                        ProgressBar.Print(currentPosition, fileSize, "Reading: Processed: ");
                    }
                }
                catch (OutOfMemoryException ex)
                {
                    ConsoleLogger.WriteError($"Please make the size of chunk smaller... Current chunk size - {BlockSizeToRead} bytes");
                    exception = ex;
                }
                catch (Exception ex)
                {
                    ConsoleLogger.WriteError(ex.Message);
                    exception = ex;
                }
                finally
                {
                    if (!ProcessIsCanceled && exception != null)
                    {
                        Console.CursorLeft = 30;
                        Console.Write("Please, wait for ending of saving the file to disk...");
                        ReadingIsCompleted = true;
                    }

                    for (int i = 0; i < CoresCount; i++)
                        Syncs[i].Event.Set();
                }
            }
        }

        private void Decompress(int threadNumber)
        {
            Syncs[threadNumber].Event.WaitOne();
            Interlocked.Increment(ref UnsyncThreads);

            BytesBlock bytesBlock = null;


            while (!ProcessIsCanceled && exception == null)
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
    

        public void WriteToTargetFile()
        {
            byte[] buffer = null;
            try
            {
                using (var targetFileStream = File.Create(TargetFilePath))
                {
                    var orderNumber = 0;
                    while (!ProcessIsCanceled && exception != null)
                    {
                        if (allDecompressIsCompleted && DictionaryToWrite.IsEmpty())
                        {
                            SavingToFileIsCompleted = true;
                            break;
                        }

                        var isSuccess = DictionaryToWrite.TryRemove(orderNumber, out buffer);
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
                ConsoleLogger.WriteError(ex.Message);
                exception = ex;
            }
        }
    }
}
