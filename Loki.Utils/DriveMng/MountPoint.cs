using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using Loki.Utils.Wmi;

namespace Loki.Utils.DriveMng
{
    public class MountPoint
    {
        [WmiProperty]
        public string Directory { get; set; }

        [WmiProperty]
        public string Volume { get; set; }

        public static List<MountPoint> Get()
        {
            return WmiHelper.Map<MountPoint>("Win32_MountPoint").ToList();
        }
    }
}
