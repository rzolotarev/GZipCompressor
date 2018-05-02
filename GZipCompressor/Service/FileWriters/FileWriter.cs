using GZipCompressor.ConfigurationManagers;
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
    public abstract class FileWriter
    {
        private const string THREAD_TIMEOUT = "ThreadTimeout";

        protected readonly string TargetFilePath;
        protected readonly int ThreadTimeout;
        protected readonly DictionaryManager<int, byte[]> DictionaryWritingManager;

        public FileWriter(string targetFilePath, DictionaryManager<int, byte[]> dictionaryWritingManager)
        {
            TargetFilePath = targetFilePath;
            ThreadTimeout = SettingsManager.GetConfigParameter<int>(THREAD_TIMEOUT);
            DictionaryWritingManager = dictionaryWritingManager;
        }

        public abstract void Save();        
    }
}
