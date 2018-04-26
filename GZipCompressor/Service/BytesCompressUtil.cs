using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZipCompressor.Service
{
    public class BytesCompressUtil
    {
        public static byte[] CompressBytes(byte[] buffer)
        {
            using (var ms = new MemoryStream())
            {
                using (var gzip = new GZipStream(ms, CompressionMode.Compress))
                {
                    gzip.Write(buffer, 0, buffer.Length);
                }
                return ms.ToArray();
            }
        }

        public static byte[] DecompressBytes(byte[] buffer,int blockSizeToRead)
        {
            using (var outputStream = new MemoryStream())
            {
                using (MemoryStream ms = new MemoryStream(buffer))
                {
                    var block = new byte[blockSizeToRead];

                    var readedBytesCount = 0;
                    using (GZipStream gz = new GZipStream(ms, CompressionMode.Decompress))
                    {
                        while ((readedBytesCount = gz.Read(block, 0, block.Length)) > 0)
                        {
                            outputStream.Write(block, 0, readedBytesCount);
                        }
                        return outputStream.ToArray();
                    }
                }
            }
        }
    }
}
