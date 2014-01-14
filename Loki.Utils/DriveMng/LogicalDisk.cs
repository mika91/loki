using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using Loki.Utils.Wmi;

namespace Loki.Utils.DriveMng
{
    public class LogicalDisk
    {
        [WmiProperty] public String DeviceID      { get; private set; }
        [WmiProperty] public String Name          { get; private set; }
        [WmiProperty] public String PNPDeviceID   { get; private set; }
        [WmiProperty] public String Status        { get; private set; }
        [WmiProperty] public int    MediaType     { get; private set; }
        [WmiProperty] public int    DriveType     { get; private set; }
        [WmiProperty(Default = -1)] public long   Size          { get; private set; }
        [WmiProperty(Default = -1)] public long   FreeSpace     { get; private set; }
        [WmiProperty] public String VolumeName    { get; private set; }

        public static List<LogicalDisk> Get()
        {
            return WmiHelper.Map<LogicalDisk>("Win32_LogicalDisk ").ToList();
        }
    }
}
