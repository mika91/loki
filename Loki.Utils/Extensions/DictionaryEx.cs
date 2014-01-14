using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loki.Utils
{
    public static class DictionaryEx
    {
        /// <summary>
        /// Get Value from a dictionary, or default one if key doesn't exist
        /// </summary>
        /// <typeparam name="TKey">Type of key</typeparam>
        /// <typeparam name="TVal">Type of value</typeparam>
        /// <param name="dico">Dictionary</param>
        /// <param name="key">Wanted key</param>
        /// <param name="defaultValue">Value returned if the key doesn't exist</param>
        /// <returns>Value associated to the key, or a default value if the key doesn't exist</returns>
        public static TVal GetOr<TKey,TVal>(this Dictionary<TKey, TVal> dico, TKey key, TVal defaultValue)
        {
            TVal result;
            return dico.TryGetValue(key, out result)
                       ? result
                       : defaultValue;
        }
    }
}
