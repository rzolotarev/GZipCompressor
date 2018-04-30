using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZipCompressor.DictionaryManager
{
    public class DictionaryManager<TKey, TValue> : Dictionary<TKey, TValue>
    {
        private readonly object _locker = new object();

        public DictionaryManager() : base() { }

        public DictionaryManager(int capacity) : base(capacity) { }

        public new void Add(TKey key, TValue value)
        {
            lock (_locker)
                base.Add(key, value);
        }

        public bool IsEmpty()
        {
            lock (_locker)
                return base.Count == 0;
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            value = default(TValue);

            lock(_locker)
            {                
                if(base.TryGetValue(key,out value))
                {
                    base.Remove(key);
                    return true;
                }
                return false;
            }
        }
    }
}
