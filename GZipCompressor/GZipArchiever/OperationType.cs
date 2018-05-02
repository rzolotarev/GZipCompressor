using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZipCompressor.GZipArchiever
{
    public enum OperationType
    {
        [Display(Name = "Compressing")]
        Compress,
        [Display(Name = "Decompressing")]
        Decompress
    }
}
