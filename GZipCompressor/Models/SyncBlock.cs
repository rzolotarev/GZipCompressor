using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GZipCompressor.Models
{
    public class SyncBlock
    {
        public AutoResetEvent Event { get; set; }
        public bool IsWorking { get; set; }

        public SyncBlock()
        {
            Event = new AutoResetEvent(false);
        }
    }
}
