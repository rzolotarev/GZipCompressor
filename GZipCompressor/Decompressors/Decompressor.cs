using GZipCompressor;
using GZipCompressor.Contracts;
using GZipCompressor.GZipArchiever;
using GZipCompressor.Models;
using GZipCompressor.Outputs;
using GZipCompressor.ProcessManagement;
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
        private int sizeBlockLength => 4;

        private bool allDecompressIsCompleted { get; set; } = false;


        public Decompressor(string sourceFilePath, string targetFilePath,
                            IThreadManager threadManager, long fileSize)
                                 : base(sourceFilePath, targetFilePath, threadManager, fileSize)
        {

        }

        public override bool Start(IFileReader fileReader, IArchiever archiever, IFileWriter fileWriter)
        {
            ConsoleLogger.WriteDiagnosticInfo($"Decompressing of {SourceFilePath} to {TargetFilePath} is started...");

            new Thread(fileReader.ReadCompressedData).Start();
           
            for (int i = 0; i < CoresCount; i++)
            {
                var currentThread = i;
                new Thread(() => archiever.Decompress(currentThread)).Start();
            }

            var threadToWrite = new Thread(fileWriter.WriteDecompressedData);
            threadToWrite.Start();

            threadToWrite.Join();
            if (StatusManager.Exception != null)
            {
                ConsoleLogger.WriteError(StatusManager.Exception.Message);
                return false;
            }

            return true;
        }                      
    }
}
