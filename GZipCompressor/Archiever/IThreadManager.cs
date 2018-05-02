using GZipCompressor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZipCompressor.Archiever
{
    public interface IThreadManager
    {
        void TryToWakeUp(SyncBlock syncBlock);

        void WakeUp(SyncBlock[] syncBlocks);
    }
}
