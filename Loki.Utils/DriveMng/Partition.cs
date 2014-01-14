using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using Loki.Utils.Wmi;

namespace Loki.Utils.DriveMng
{
    public class Partition
    {
        [WmiProperty]public String   DeviceID    { get; set; }
        [WmiProperty(Default = -1)]public long     Size        { get; set; }
        [WmiProperty]public string   Type        { get; set; }
        [WmiProperty(Default = -1)]public int      Index       { get; set; }
        [WmiProperty(Default = -1)]public int      DiskIndex   { get; set; }
        [WmiProperty("PrimaryPartition")]
        public bool Primary { get; set; }
      
        public static List<Partition> Get()
        {
            return WmiHelper.Map<Partition>("Win32_DiskPartition").ToList();
        }
    }
}
