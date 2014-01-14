using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loki.Utils
{
    public static class TypeEx
    {
        /// <summary>
        /// Return all parents type (both inteface and base type)
        /// </summary>
        public static IEnumerable<Type> GetParentTypes(this Type type, bool includeItSelf = true)
        {
            // is there any base type?
            if ((type == null) || (type.BaseType == null))
            {
                yield break;
            }

            // return itself
            if (includeItSelf)
                yield return type;

            // return all implemented or inherited interfaces
            foreach (var i in type.GetInterfaces())
            {
                yield return i;
            }

            // return all inherited types
            var currentBaseType = type.BaseType;
            while (currentBaseType != null)
            {
                yield return currentBaseType;
                currentBaseType = currentBaseType.BaseType;
            }
        }
    }
}
