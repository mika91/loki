using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loki.Utils.Extensions
{
    public static class IEnumerableEx
    {
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (var item in collection)
                action(item);
        }

    }
}
