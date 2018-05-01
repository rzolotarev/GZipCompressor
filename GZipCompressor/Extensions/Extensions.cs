using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZipCompressor.Extensions
{
    public static class Extensions
    {
        public static bool CanCreate(this FileInfo fileInfo)
        {
            string file = Path.Combine(fileInfo.Directory.ToString(), Guid.NewGuid().ToString() + ".tmp");            
            while (File.Exists(file))
            {
                file += "1";
            };
            
            var canCreate = true;
            try
            {
                using (File.Create(file)) { }
                File.Delete(file);                
            }
            catch
            {
                canCreate = false;
            }

            return canCreate;
        }
    }
}
