using GZipCompressor.ConfigurationManagers;
using GZipCompressor.Contracts;
using GZipCompressor.DictionaryManager;
using GZipCompressor.ProcessManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GZipCompressor.Service
{
    public class FileWriter : IFileWriter
    {
        private const string THREAD_TIMEOUT = "ThreadTimeout";

        private readonly string _targetFilePath;
        private readonly int _threadTimeout;
        private readonly DictionaryManager<int, byte[]> _dictionaryWritingManager;

        public FileWriter(string targetFilePath, DictionaryManager<int, byte[]> dictionaryWritingManager)
        {
            _targetFilePath = targetFilePath;
            _threadTimeout = SettingsManager.GetConfigParameter<int>(THREAD_TIMEOUT);
            _dictionaryWritingManager = dictionaryWritingManager;
        }

        public void WriteCompressedData()
        {
            var orderNumber = 0;
            byte[] bytesBlock = null;

            try
            {
                using (var targetFileStream = File.Create(_targetFilePath))
                {
                    while (!StatusManager.ProcessIsCanceled && StatusManager.Exception == null)
                    {
                        if (StatusManager.AllCompressIsCompleted && _dictionaryWritingManager.IsEmpty())
                        {
                            StatusManager.SavingToFileIsCompleted = true;
                            break;
                        }

                        var isSuccess = _dictionaryWritingManager.TryRemove(orderNumber, out bytesBlock);
                        if (!isSuccess)
                        {
                            Thread.Sleep(_threadTimeout);
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

        public void WriteDecompressedData()
        {
            byte[] buffer = null;
            try
            {
                using (var targetFileStream = File.Create(_targetFilePath))
                {
                    var orderNumber = 0;
                    while (!StatusManager.ProcessIsCanceled && StatusManager.Exception == null)
                    {
                        if (StatusManager.AllDecompressIsCompleted && _dictionaryWritingManager.IsEmpty())
                        {
                            StatusManager.SavingToFileIsCompleted = true;
                            break;
                        }

                        var isSuccess = _dictionaryWritingManager.TryRemove(orderNumber, out buffer);
                        if (!isSuccess)
                        {
                            Thread.Sleep(_threadTimeout);
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
