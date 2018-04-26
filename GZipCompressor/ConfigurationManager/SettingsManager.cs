using Outputs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZipCompressor.ConfigurationManagers
{
    public class SettingsManager
    {        
        public static T GetConfigParameter<T>(string field)
        {
            var appSettings = ConfigurationManager.AppSettings;

            try
            {                
                var chunkSizeToRead = (T)Convert.ChangeType(appSettings.Get(field), typeof(T));
                return chunkSizeToRead;
            }
            catch(Exception ex)
            {
                ConsoleLogger.WriteError(ex.Message);
                throw ex;
            }
        }
    }     
}
