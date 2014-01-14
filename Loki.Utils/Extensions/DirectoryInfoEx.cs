using System;
using System.IO;
using System.Runtime.InteropServices;
using Loki.Utils.DriveMng;

namespace Loki.Utils.Extensions
{
    public static class DirectoryInfoEx
    {
        public static bool GetFreeSpace(this DirectoryInfo di, out ulong free, out ulong total)
        {
            return DriveHelper.DriveFreeBytes(di.FullName, out free, out total);
        }

        public static long GetFreeSpace(this DirectoryInfo di)
        {
            return DriveHelper.GetFreeSpace(di.FullName);
        }

        public static long GetUsedSpace(this DirectoryInfo di)
        {
            return DriveHelper.GetUsedSpace(di.FullName);
        }
    }

}
