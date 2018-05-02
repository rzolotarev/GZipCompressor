using GZipCompressor;
using GZipCompressor.Contracts;
using GZipCompressor.GZipArchiever;
using GZipCompressor.Models;
using GZipCompressor.Outputs;
using GZipCompressor.ProcessManagement;
using GZipCompressor.Service;
using Outputs;
using System;
using System.IO;
using System.Threading;

namespace Compressors
{
    public class Compressor : GZipBlockArchiver
    {
        private static bool allCompressIsCompleted { get; set; } = false;        

        public Compressor(string sourceFilePath, string targetFilePath,
                          IThreadManager threadManager, long fileSize)
                                     : base(sourceFilePath, targetFilePath, threadManager, fileSize)
        {
        }

        public override bool Start(IFileReader fileReader, IArchiever archiever, IFileWriter fileWriter)
        {
            ConsoleLogger.WriteDiagnosticInfo($"Compressing of {SourceFilePath} to {TargetFilePath} is started...");


            new Thread(fileReader.ReadUncompressedData).Start();

            var compressThreads = new Thread[CoresCount];
           
            for (int i = 0; i < CoresCount; i++)
            {
                var current = i;
                new Thread(() => archiever.Compress(current)).Start();
            }

            var threadToWrite = new Thread(fileWriter.WriteCompressedData);
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
