using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using MusicBackup.Entities;
using MusicBackup.dMC;
using log4net;
using Loki.Utils;

namespace MusicBackup
{
    public class AudioManager
    {
        private static ILog Log = LogManager.GetLogger(typeof(AudioManager));

        #region Folder to folder backup

        /// <summary>
        /// Simple Audio libbray backup method
        /// </summary>
        /// <param name="srcPath"></param>
        /// <param name="destPath"></param>
        /// <param name="simulateOnly"></param>
        public List<BackupResult> Backup(string srcPath, string destPath, bool simulateOnly = false)
        {
            try
            {
                // Launch backup
                var results = backup(new DirectoryInfo(srcPath), new DirectoryInfo(destPath), "mp3", 320, simulateOnly).ToList();
                while (dMCConverter.Instance.IsRunning)
                {
                    Thread.Sleep(2000);
                    Log.Debug(() => "AudioConverter is running...");
                }
                Log.Info(() => "Backup done !!!");
                return results;
            }
            catch (Exception ex)
            {
                Log.Fatal(() => ex.Message);
                throw;
            }
        }


        private IEnumerable<BackupResult> backup(DirectoryInfo src, DirectoryInfo dest, String format, int bitrate, bool simulateOnly)
        {
            Log.Info(() => "Backup folder <{0}> => <{1}>", src.FullName, dest.FullName);

            // *******************************
            // Source Folder
            // *******************************
            if (!src.Exists)
                throw new ArgumentException("Source directory doesn't exist.", src.FullName);

            // *******************************
            // Destination Folder
            // *******************************
            if (!dest.Exists)
            {
                Log.Warn(() => "Destination directory <{0}> doesn't exist.", dest.FullName);
                Log.Info(() => "Trying to create destination directory <{0}>", dest.FullName);
                if (!simulateOnly)
                    dest.Create();
            }

            // *******************************
            // Backup files
            // *******************************
            var files = src.GetFiles()
                           .OrderBy(x => x.FullName)
                           .ToList();

            foreach (var srcFile in files)
            {
                var srcAInfo = AudioInfo.Get(srcFile);
                if (srcAInfo != null)
                {
                    // Dest files exist?
                    var destFile = new FileInfo(srcFile.FullName
                                                       .Replace(src.FullName, dest.FullName)
                                                       .Replace(srcFile.Extension, "." + format));
                    var destAInfo = AudioInfo.Get(destFile);
                    if (destAInfo == null || destAInfo.Format != format)
                    {
                        // Backup single file
                        if (srcAInfo.Format == format)
                        {
                            // Simple copy
                            Log.Info(() => "Copy <{0}> => <{1}>", srcFile.FullName, destFile.FullName);
                            if (!simulateOnly)
                                File.Copy(srcFile.FullName, destFile.FullName);
                            yield return
                                new BackupResult()
                                {
                                    Action = BackupResult.ActionType.Copied,
                                    Source = null // TODO
                                };

                        }
                        else
                        {
                            // Convert
                            Log.Info(() => "Convert <{0}> => <{1}>", srcFile.FullName, destFile.FullName);
                            if (!simulateOnly)
                                dMCConverter.Instance.ConvertToMP3(srcFile.FullName, destFile.FullName, bitrate);
                            yield return
                                new BackupResult()
                                {
                                    Action = BackupResult.ActionType.Converted,
                                    Source = null // TODO
                                };

                        }
                    }
                    else
                    {
                        Log.Debug(() => "File <{0}> has already a backup", srcFile.FullName);
                        yield return
                                new BackupResult()
                                {
                                    Action = BackupResult.ActionType.Ignored,
                                    Source = null // TODO
                                };
                    }
                }
                else
                    Log.Debug(() => "File <{0}> is not an audio file", srcFile.FullName);

            }

            // *******************************
            // Recursively backup directories
            // *******************************
            var subdirs = src.GetDirectories()
                             .OrderBy(x => x.FullName)
                             .ToList();

            foreach (var srcSubdir in subdirs)
            {
                // dest dir exists?
                var destSubdir = new DirectoryInfo(srcSubdir.FullName.Replace(src.FullName, dest.FullName));
                if (!destSubdir.Exists)
                {
                    Log.Warn(() => "Destination directory <{0}> doesn't exist.", destSubdir);
                    Log.Info(() => "Trying to create destination directory <{0}>", destSubdir);
                    dest.Create();
                }

                // backup subdir
                var results = backup(srcSubdir, destSubdir, format, bitrate, simulateOnly);
                foreach (var result in results)
                    yield return result;
            }
        }

        #endregion

        #region Library Backup

        /// <summary>
        /// Backup a libray to another one
        /// </summary>
        public static IEnumerable<BackupResult> Backup(AudioLibrary srcLib, AudioLibrary destLib, bool simulate = true, bool force = false)
        {
            Log.Info(()=>"***************************************");
            Log.Info(()=>"Backup Music Library:");
            Log.Info(()=>"\tFrom: {0}", srcLib.Root);
            Log.Info(()=>"\tTo  : {0}", destLib.Root);
            if (simulate) Log.Info(() => "\tSIMULATE MODE (no written files)");
            if (force)    Log.Info(() => "\tFORCE MODE    (overwrite existing files)");
            Log.Info(() => "***************************************");

            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Get all src items
            var srcItems = srcLib.GetAllItems().ToDictionary(x => x.FilePath);

            // Get all dest items
            var destItems = destLib.GetAllItems().ToDictionary(x => x.FilePath);

            // warning on dest files not included in src
            if (Log.IsDebugEnabled)
            {
                foreach (var destFile in destItems)
                {
                    var filename = PathConvert(destFile.Key, destLib.Root, srcLib.Root);
                    if (!srcItems.ContainsKey(filename))
                        Log.DebugFormat("File {0} is not present on the library.", destFile);
                }
            }


            // Iterate src files
            foreach (var src in srcLib.GetAllItems())
            {
                // Expected output name
                var output = (src.AudioInfo == null)
                                 ? PathConvert(src.FilePath, srcLib.Root, destLib.Root)
                                 : src.AudioInfo.Format.Contains("mp3")
                                       ? PathConvert(src.FilePath, srcLib.Root, destLib.Root)
                                       : PathConvert(src.FilePath, srcLib.Root, destLib.Root).Replace(src.Extension, ".mp3");


                // Do jobs
                LibItem dest;
                if (destItems.TryGetValue(output, out dest) && !force)
                {
                    Log.Debug(() => "Ignoring already existing file: {0}", output);
                    yield return new BackupResult() { Action = BackupResult.ActionType.Ignored, Source = src , Dest = dest};
                }
                else
                {
                    if (src.AudioInfo == null)
                    {
                        Log.Info(() => "Copying misc file: {0}", output);
                        SafeCopy(src.FilePath, output);
                        destLib.Add(output);
                        yield return new BackupResult() { Action = BackupResult.ActionType.Copied, Source = src, Dest = dest };
                    }
                    else
                    {
                        var audio = src.AudioInfo;
                        if (!audio.Format.Contains("mp3"))
                        {
                            Log.Info(() => "Converting music file: {0}", output);
                            dMCConverter.Instance.ConvertToMP3(src.FilePath, output);
                            destLib.Add(output);
                            yield return new BackupResult() { Action = BackupResult.ActionType.Converted, Source = src, Dest = dest };
                        }
                        else
                        {
                            Log.Info(() => "Copying music file: {0}", output);
                            SafeCopy(src.FilePath, output);
                            destLib.Add(output);
                            yield return new BackupResult() { Action = BackupResult.ActionType.Copied, Source = src, Dest = dest };
                        }
                    }
                }
            }

            // waiting for convert done
            while (dMCConverter.Instance.IsRunning)
            {
                Thread.Sleep(1000);
                Log.Debug( () => "Waiting for AudioConverter jobs to be finished - Started {0}s ago...", sw.ElapsedMilliseconds /1000);
            }

            // Return 
            sw.Stop();
            Log.Info( () => "Library backup completed in {0}s", sw.ElapsedMilliseconds / 1000);
        }

        static string PathConvert(String filename, string srcRoot, string destRoot)
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

        #endregion

    }


}
