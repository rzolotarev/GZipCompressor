using GZipCompressor.ConfigurationManagers;
using GZipCompressor.Contracts;
using GZipCompressor.Models;
using GZipCompressor.Outputs;
using GZipCompressor.ProcessManagement;
using GZipCompressor.QueueManager;
using Outputs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZipCompressor.Service
{
    public class FileReader : IFileReader
    {
        private const string CHUNK_SIZE = "ChunkSize";
        private const int sizeBlockLength = 4;

        private readonly int _blockSizeToRead;
        private readonly string _sourceFilePath;
        private readonly QueueManager<BytesBlock>[] _compressedDataManagers;
        private int processorsCount => Environment.ProcessorCount;

        public FileReader(string sourceFilePath, QueueManager<BytesBlock>[] compressedDataManagers)
        {
            _blockSizeToRead = SettingsManager.GetConfigParameter<int>(CHUNK_SIZE);
            _sourceFilePath = sourceFilePath;
            _compressedDataManagers = compressedDataManagers;
        }

        public void ReadUncompressedData()
        {
            long currentPosition = 0;
            var readedBytesCount = 0;
            var blockNumber = 0;
            var queueNumber = 0;

            byte[] buffer = new byte[_blockSizeToRead];

            try
            {
                using (var sourceFileStream = new FileStream(_sourceFilePath, FileMode.Open, FileAccess.Read,
                                                        FileShare.Read, _blockSizeToRead))
                {
                    long fileSize = sourceFileStream.Length;

                    while (((readedBytesCount = sourceFileStream.Read(buffer, 0, buffer.Length)) > 0)
                        && !StatusManager.ProcessIsCanceled && StatusManager.Exception == null)
                    {
                        if (readedBytesCount < _blockSizeToRead)
                            Array.Resize(ref buffer, readedBytesCount);

                        queueNumber = queueNumber % processorsCount;
                        _compressedDataManagers[queueNumber].Enqueue(new BytesBlock(buffer, blockNumber++));
                        queueNumber++;

                        currentPosition += readedBytesCount;
                        ProgressBar.Print(currentPosition, fileSize, "Reading: Processed: ");
                    }
                }
            }
            catch (OutOfMemoryException ex)
            {
                ConsoleLogger.WriteError($"Please make the size of chunk smaller... Current chunk size - {_blockSizeToRead} bytes");
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

                foreach (var dataManager in _compressedDataManagers)
                    dataManager.WakeUp();
            }
        }

        public void ReadCompressedData()
        {
           using (var sourceFileStream = new FileStream(_sourceFilePath, FileMode.Open))
                {
                    long fileSize = sourceFileStream.Length;
                    long currentPosition = 0;
                    var orderNumber = 0;
                    var queueNumber = 0;
                    var bufferForLength = new byte[sizeBlockLength];

                    try
                    {
                        while ((sourceFileStream.Read(bufferForLength, 0, bufferForLength.Length) > 0)
                            && !StatusManager.ProcessIsCanceled && StatusManager.Exception == null)
                        {
                            byte[] buffer = new byte[BitConverter.ToInt32(bufferForLength, 0)];
                            sourceFileStream.Read(buffer, 0, buffer.Length);

                            queueNumber = queueNumber % processorsCount;
                            _compressedDataManagers[queueNumber].Enqueue(new BytesBlock(buffer, orderNumber++));
                            queueNumber++;

                            currentPosition += buffer.Length + sizeBlockLength;
                            ProgressBar.Print(currentPosition, fileSize, "Reading: Processed: ");
                        }
                    }
                    catch (OutOfMemoryException ex)
                    {
                        ConsoleLogger.WriteError($"Please make the size of chunk smaller... Current chunk size - {_blockSizeToRead} bytes");
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

                        foreach (var dataManager in _compressedDataManagers)
                            dataManager.WakeUp();
                    }
                }            
        }
    }
}
