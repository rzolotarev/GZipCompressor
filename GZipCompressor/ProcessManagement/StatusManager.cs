using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZipCompressor.ProcessManagement
{
    public static class StatusManager
    {
        public static bool ProcessIsCanceled { get; set; }
        public static Exception Exception { get; set; }
        public static bool ReadingIsCompleted { get; set; }
        public static bool AllCompressIsCompleted { get; set; }
        public static bool SavingToFileIsCompleted { get; set; }
        public static bool AllDecompressIsCompleted { get; set; }        
    }
}
