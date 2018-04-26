using System.Linq;

namespace GZipCompressor.Models
{
    public class BytesBlock
    {        
        public int OrderNumber { get; private set; }        
        public byte[] Buffer { get; private set; }

        public BytesBlock(byte[] buffer, int orderNumber)
        {
            Buffer = buffer.ToArray();
            OrderNumber = orderNumber;                                   
        }
    }
}
