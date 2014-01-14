using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using MusicBackup.Entities;
using log4net;
using Loki.Utils;

namespace MusicBackup
{
    public class AudioManager
    {
        private static ILog Log = LogManager.GetLogger(typeof(AudioManager));

        /// <summary>
        /// Backup a libray to another one
        /// </summary>
        /// <param name="srcLib"></param>
        /// <param name="destPath"></param>
        /// <param name="simulate"></param>
        /// <param name="force"></param>
        public static IEnumerable<BackupResult> Backup(AudioLibrary srcLib, string destRoot, bool simulate = true, bool force = false)
        {
            Console.WriteLine("\r\n***************************************");
            Console.WriteLine("Backup Music Library:");
            Console.WriteLine("\tFrom: {0}", srcLib.Root);
            Console.WriteLine("\tTo  : {0}", destRoot);
            if (simulate) Console.WriteLine("=> SIMULATE MODE (no written files)");
            if (force) Console.WriteLine("=> FORCE MODE    (overwrite existing files)");
            Console.WriteLine("***************************************\r\n");

            Stopwatch sw = new Stopwatch();
            sw.Start();


            // Get all src items
            var srcItems = srcLib.GetAllFiles().ToDictionary(x => x.FilePath);

            // Get all dest files
            var destFiles = AudioLibrary.GetFilesOnDisk(new DirectoryInfo(destRoot)).ToList();

            // warning on dest files not included in src
            if (Log.IsDebugEnabled)
            {
                foreach (var destFile in destFiles)
                {
                    var filename = PathConvert(destFile, destRoot, srcLib.Root);
                    if (!srcItems.ContainsKey(filename))
                        Log.DebugFormat("File {0} is not present on the library.", destFile);
                }
            }
           

            // Iterate src files
            foreach (var src in srcLib.GetAllFiles())
            {
                // Expected output name
                var output = src is ItemMisc
                                 ? PathConvert(src.FilePath, srcLib.Root, destRoot)
                                 : (src as ItemAudio).Format.Contains("mp3")
                                       ? PathConvert(src.FilePath, srcLib.Root, destRoot)
                                       : PathConvert(src.FilePath, srcLib.Root, destRoot).Replace(src.Extension, ".mp3");
                
                // Do jobs
                if (File.Exists(output) && !force)
                {
                    Log.Debug(()=>"=> Ignoring already existing file: {0}", output);
                    yield return new BackupResult() { Action = BackupResult.ActionType.Ignored, InPath = src.FilePath, OutPath = output };
                }
                else
                {
                    if (src is ItemMisc)
                    {
                        Log.Info(() => "=> Copying misc file: {0}", output);
                        SafeCopy(src.FilePath, output);
                        yield return new BackupResult() { Action = BackupResult.ActionType.Copied, InPath = src.FilePath, OutPath = output };
                    }
                    else
                    {
                        var audio = src as ItemAudio;
                        if (!audio.Format.Contains("mp3"))
                        {
                            Log.Info(() => "=> Converting music file: {0}", output);
                            AudioConverter.Instance.ConvertToMP3(src.FilePath, output);
                            yield return new BackupResult() { Action = BackupResult.ActionType.Converted, InPath = src.FilePath, OutPath = output };
                        }
                        else
                        {
                            Log.Info(() => "=> Copying music file: {0}", output);
                            SafeCopy(src.FilePath, output);
                            yield return new BackupResult() { Action = BackupResult.ActionType.Copied, InPath = src.FilePath, OutPath = output };
                        }
                    }
                }
            }

            // waiting for convert done
            while (AudioConverter.Instance.IsRunning)
            {
                Thread.Sleep(1000);
                Console.WriteLine("Waiting for AudioConverter jobs to be finished");
            }

            // Return 
            sw.Stop();
            Console.WriteLine("Comparing library completed in {0}s", sw.ElapsedMilliseconds / 1000);
        }

        static string PathConvert(String filename, string srcRoot,  string destRoot)
        {
            return filename.Replace(srcRoot, destRoot);
        }

        static void SafeCopy(String src, string dest)
        {
            var destInfo = new FileInfo(dest);
            if (!Directory.Exists(destInfo.DirectoryName))
                Directory.CreateDirectory(destInfo.DirectoryName);
            File.Copy(src, dest, true);
        }

    }


}
