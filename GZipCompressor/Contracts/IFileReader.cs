﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZipCompressor.Contracts
{
    public interface IFileReader
    {
        void ReadUncompressedData();
        void ReadCompressedData();
    }
}