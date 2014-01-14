using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Loki.Utils.DriveMng
{
    public class DriveHelper
    {
        #region Disk space usage

        /// http://www.pinvoke.net/default.aspx/kernel32.GetDiskFreeSpaceEx
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
                                                      out ulong lpFreeBytesAvailable,
                                                      out ulong lpTotalNumberOfBytes,
                                                      out ulong lpTotalNumberOfFreeBytes);



        /// <summary>
        /// Determines the ammount of free space on a drive (local or remote) in bytes
        /// </summary>
        /// <param name="folderName">Directory or unc folder name of the volume to
        /// be checked (can be a networked drive)</param>
        /// <param name="freespace">Ammount of freespace is returned here in bytes, 0 
        /// if the value could not be obtained</param>
        /// <param name="totalsize">Disk total space is returned here in bytes, 0 
        /// if the value could not be obtained</param>
        /// <returns>true if the method successfully retrieved the ammount of freespace
        /// on the given drive, false  otherwise</returns>
        public static bool DriveFreeBytes(string folderName, out ulong freespace, out ulong totalsize)
        {
            freespace = 0;
            totalsize = 0;

            if (string.IsNullOrEmpty(folderName))
            {
                throw new ArgumentNullException("folderName");
            }

            if (!folderName.EndsWith("\\"))
            {
                folderName += '\\';
            }

            ulong free, total, dummy = 0;

            if (GetDiskFreeSpaceEx(folderName, out free, out total, out dummy))
            {
                freespace = free;
                totalsize = total;
                return true;
            }

            return false;
        }


        /// <summary>
        /// Return availbale space on a directory.
        /// </summary>
        /// <param name="dir">Directory to check.</param>
        /// <returns>Free space on the volume containing <paramref name="dir"/>, or -1 if it doesn't exist.</returns>
        public static long GetFreeSpace(string dir)
        {
            ulong freespace = 0;
            ulong totalsize = 0;

            var exists = DriveFreeBytes(dir, out freespace, out totalsize);

            return exists ? (long)freespace : -1;
        }

        /// <summary>
        /// Return space used by a directory.
        /// </summary>
        /// <param name="dir">Directory to check.</param>
        /// <returns>Used space by the folder <paramref name="dir"/>, or -1 if it doesn't exist.</returns>
        public static long GetUsedSpace(string dir)
        {
            ulong freespace = 0;
            ulong totalsize = 0;

            var exists = DriveFreeBytes(dir, out freespace, out totalsize);

            return exists ? (long)totalsize : -1;
        }


        #endregion
    }

}
