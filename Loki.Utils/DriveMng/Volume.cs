using System;
using System.Collections.Generic;
using System.Linq;
using Loki.Utils.Wmi;

namespace Loki.Utils.DriveMng
{
    public class Volume
    {
        [WmiProperty] public String   DeviceID        { get; set; }
        [WmiProperty] public String   Caption         { get; set; }
        [WmiProperty] public String Name { get; set; }
        [WmiProperty] public string FileSystem { get; set; }
        [WmiProperty] public bool QuotasEnabled { get; set; }
        [WmiProperty] public int DriveType { get; set; }
        [WmiProperty] public long FreeSpace { get; set; }
        [WmiProperty("BootVolume")] public bool IsBootVolume { get; set; }
     //   [WmiProperty(Name = "Capacity", Default = -1)]   public long Size         { get; set; }
        [WmiProperty("Capacity", -1)]
        public long Size { get; set; }


        public static List<Volume> Get()
        {
            return WmiHelper.Map<Volume>("Win32_Volume").ToList();
        }
    }
}
