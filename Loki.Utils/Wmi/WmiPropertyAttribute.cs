using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loki.Utils.Wmi
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public class WmiPropertyAttribute : Attribute
    {
        public String Name { get; set; }
        public Object Default { get; set; }

        public WmiPropertyAttribute()
        {
        }

        public WmiPropertyAttribute(String name)
        {
            Name = name;
        }

        public WmiPropertyAttribute(String name, object defaultValue)
        {
            Name = name;
            Default = defaultValue;
        }
    }
}
