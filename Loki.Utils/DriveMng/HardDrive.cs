using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using Loki.Utils.Wmi;
using Loki.Utils;
using log4net;

namespace Loki.Utils.DriveMng
{
    public class HardDrive
    {
        [WmiProperty] public String   DeviceID        { get; private set; }
        [WmiProperty] public String   Model           { get; private set; }
        [WmiProperty] public String   Name            { get; private set; }
        [WmiProperty(Default = -1)] public int      Partitions      { get; private set; }
        [WmiProperty] public String   PNPDeviceID     { get; private set; }
        [WmiProperty] public String   SerialNumber    { get; private set; }
        [WmiProperty(Default = -1)] public long     Size            { get; private set; }
        [WmiProperty] public String   Status          { get; private set; }
        [WmiProperty] public String   MediaType       { get; private set; }

        public static List<HardDrive> Get()
        {

            return WmiHelper.Map<HardDrive>("Win32_DiskDrive").ToList();

        }
    }

   

}
