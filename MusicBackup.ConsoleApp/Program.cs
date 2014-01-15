using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MusicBackup.Entities;
using log4net;
using Loki.Utils;


namespace MusicBackup.ConsoleApp
{
    class Program
    {
        private static ILog Log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            //Log.Info(() => "\r\n*************************");
            //Log.Info(() => "***      BACKUP      ****");
            //Log.Info(() => "*************************\r\n");

            //var mng = new Class1();
            //var resutls =
            //    mng.Backup(
            //        Properties.Settings.Default.Source,
            //        Properties.Settings.Default.Destination,
            //        false).ToList();


            //Log.Info(() => "*************************");
            //Log.Info(() => "***     RESULTS      ****");
            //Log.Info(() => "*************************");

            //Log.Info(() => "Music files converted = {0}", resutls.Count(x => x.Action == BackupResult.ActionType.Converted));
            //Log.Info(() => "Music files copied    = {0}", resutls.Count(x => x.Action == BackupResult.ActionType.Copied));
            //Log.Info(() => "Music files ignored   = {0}", resutls.Count(x => x.Action == BackupResult.ActionType.Ignored));
            //Log.Info(() => "Music files total     = {0}", resutls.Count(x => x.Action == BackupResult.ActionType.Copied));

            // Read configuration
            var src_path = new DirectoryInfo(Properties.Settings.Default.Source);
            var dest_path = new DirectoryInfo(Properties.Settings.Default.Destination);
            var nbCores = Convert.ToInt32(Properties.Settings.Default.NbCores);

            // Init converter
            //AudioConverter.Instance.NbCores = 3;    // nbCores;     // TODO


            List<ScanResult> scanResults = null;

            // Import source library
            Log.Info(() => "Trying to load source libray...");
            var srcLib = AudioLibrary.Load("src.content");
            if (srcLib == null)
            {
                Log.Info(() => "No library found. Create a new one");
                scanResults = AudioLibrary.Create(src_path.FullName, out srcLib);
            }
            else if (new DirectoryInfo(srcLib.Root).FullName != src_path.FullName) // TODO securise if path doen't exist
            {
                Log.Info(() => "Library root folder doesn't match. Create a new one");
                scanResults = AudioLibrary.Create(src_path.FullName, out srcLib); 
            }
            else
            {
                Log.Info(() => "Scanning source library...");
                scanResults = srcLib.Scan(false);
            }
            PrintScanRes(scanResults);

            // Import libraries
            Log.Info(() => "Trying to load backup libray...");
            var destLib = AudioLibrary.Load("dest.content");
            if (destLib == null)
            {
                Log.Info(() => "No library found. Create a new one");
                scanResults = AudioLibrary.Create(dest_path.FullName, out destLib);
            }
            else if (new DirectoryInfo(destLib.Root).FullName != dest_path.FullName) // TODO securise if path doen't exist
            {
                Log.Info(() => "Library root folder doesn't match. Create a new one");
                scanResults = AudioLibrary.Create(dest_path.FullName, out destLib);
            }
            else
            {
                Log.Info(() => "Scanning destination library...");
                scanResults = destLib.Scan(false);
            }
            PrintScanRes(scanResults);

            // Backing up library
            var results = AudioManager.Backup(srcLib, destLib, true, false).ToList();
            PrintBackupRes(results);

            // Saving Libraries
            srcLib.Save("src.content");
            destLib.Save("dest.content");

            //// Exit
            //Console.WriteLine("Press a key to exit..");
            //Console.ReadLine();
        }

        public static void PrintBackupRes(List<BackupResult> results)
        {
            Log.Info(() => "*************************");
            Log.Info(() => "***     RESULTS      ****");
            Log.Info(() => "*************************");

            var audioFiles  = results.Where(x => x.Source.AudioInfo != null).ToList();
            var miscFiles   = results.Where(x => x.Source.AudioInfo == null).ToList();
           
            Log.Info(() => "");
            Log.Info(() => "Music files converted = {0}", audioFiles.Count(x => x.Action == BackupResult.ActionType.Converted));
            Log.Info(() => "Music files copied    = {0}", audioFiles.Count(x => x.Action == BackupResult.ActionType.Copied));
            Log.Info(() => "Music files ignored   = {0}", audioFiles.Count(x => x.Action == BackupResult.ActionType.Ignored));
            Log.Info(() => "Music files total     = {0}", audioFiles.Count());

            Log.Info(() => "");
            Log.Info(() => "Misc files copied    = {0}" , miscFiles.Count(x => x.Action == BackupResult.ActionType.Copied));
            Log.Info(() => "Misc files ignored   = {0}" , miscFiles.Count(x => x.Action == BackupResult.ActionType.Ignored));
            Log.Info(() => "Misc files total     = {0}" , miscFiles.Count());
        }

        // TODO -- scan result both misc & audio files
        public static void PrintScanRes(List<ScanResult> results)
        {
            Log.Info(() => "*************************");
            Log.Info(() => "***  Scan Results    ****");
            Log.Info(() => "*************************");

            Log.Info(() => "");
            Log.Info(() => "Files added     = {0}", results.Count(x => x.Action == ScanResult.ActionType.Added));
            Log.Info(() => "Files deleted   = {0}", results.Count(x => x.Action == ScanResult.ActionType.Deleted));
            Log.Info(() => "Files ignored   = {0}", results.Count(x => x.Action == ScanResult.ActionType.Ignored));
            Log.Info(() => "Files updated   = {0}", results.Count(x => x.Action == ScanResult.ActionType.Updated));
            Log.Info(() => "Files total     = {0}", results.Count());
        }

        //static void Main2(string[] args)
        //{
        //    // Read configuration
        //    var src_path  = new DirectoryInfo(Properties.Settings.Default.Source);
        //    var dest_path = new DirectoryInfo(Properties.Settings.Default.Destination);
        //    var nbCores   = Convert.ToInt32(Properties.Settings.Default.NbCores);
        //    var bitrate   = Convert.ToInt32(Properties.Settings.Default.Bitrate);

        //    // Init converter
        //    AudioConverter.Instance.Bitrate = bitrate;
        //    AudioConverter.Instance.NbCores = nbCores;

        //    // Process
        //    var result = new Result();
        //    var sw = new Stopwatch();
        //    sw.Start();
        //    DoBackup(src_path, dest_path, result);
        //    sw.Stop();
        //    result.Duration = sw.ElapsedMilliseconds;

        //    // Print Results
        //    Console.WriteLine("");
        //    Console.WriteLine("--------- Results ----------");
        //    Console.WriteLine("");
        //    Console.WriteLine("Directory created     = {0}", result.NbCreatedDir);
        //    Console.WriteLine("");
        //    Console.WriteLine("Music files converted = {0}", result.NbConvertedMusic);
        //    Console.WriteLine("Music files copied    = {0}", result.NbCopiedMusic);
        //    Console.WriteLine("Music files ignored   = {0}", result.NbIgnoredMusic);
        //    Console.WriteLine("Music files total     = {0}", result.NbTotalMusic);
        //    Console.WriteLine("");                    
        //    Console.WriteLine("Misc files copied     = {0}", result.NbCopiedMisc);
        //    Console.WriteLine("Misc files ignored    = {0}", result.NbIgnoredMisc);
        //    Console.WriteLine("Misc files total      = {0}", result.NbTotalMisc);
        //    Console.WriteLine("\r\n => Ellapsed time: {0}s", result.Duration/1000);
        //    Console.WriteLine("----------------------------");

        //    // Exit
        //    Console.WriteLine("Press a key to exit..");
        //    Console.ReadLine();
        //}


        //static void DoBackup(DirectoryInfo src, DirectoryInfo dest, Result result, bool force = false)
        //{
        //    // Verify source folder
        //    if (!src.Exists)
        //    {
        //        Console.WriteLine("No source directory found: {0}", src.FullName);
        //        result.Success = false;
        //        return;
        //    }

        //    // verify destination folder
        //    if (!dest.Exists)
        //    {
        //        Console.WriteLine("=> Creating missing destination directory: {0}", dest.FullName);
        //        dest.Create();
        //        result.NbCreatedDir++;
        //    }

        //    // List files
        //    var srcFiles = src.GetFiles().OrderBy(x => x.Name).ToList();
        //    var destFiles = dest.GetFiles().OrderBy(x => x.Name).ToList();

        //    foreach (var srcFile in srcFiles)
        //    {
        //        var props = AudioProperties.Read(srcFile.FullName);
        //        var destFile = srcFile.FullName.Replace(src.FullName, dest.FullName);
        //        var format = props["Type"];
        //        var isMusicFile = !String.IsNullOrEmpty(format);

        //        // files already exists?
        //        if (destFiles.Any(x => x.Name == srcFile.Name) && !force)
        //        {
        //            if (!isMusicFile)
        //            {
        //                Console.WriteLine("Ignoring misc file: {0}", srcFile);
        //                result.NbIgnoredMisc++;
        //            }
        //            else
        //            {
        //                Console.WriteLine("Ignoring music file: {0}", srcFile);
        //                result.NbIgnoredMusic++;
        //            }
        //        }
        //        else
        //        {
        //            if (!isMusicFile)
        //            {
        //                // Simple copy
        //                Console.WriteLine("=> Copy misc file: {0}", destFile);
        //                File.Copy(srcFile.FullName, destFile);
        //                result.NbCopiedMisc++;
        //            }
        //            else
        //            {
        //                // Display FileInfo
        //                Console.WriteLine("\r\n-------------------------------------");
        //                Console.WriteLine("New music file found: {0}", srcFile.FullName);
        //                props.ToList().ForEach(x => Console.WriteLine("\t{0}: {1}", x.Name, x.Value));
        //                Console.WriteLine("-------------------------------------\r\n");

        //                // Process file
        //                if (format.Contains("mp3"))
        //                {
        //                    // Simple copy
        //                    Console.WriteLine("=> Copy music file: {0}", destFile);
        //                    File.Copy(srcFile.FullName, destFile);
        //                    result.NbCopiedMusic++;
        //                }
        //                else
        //                {
        //                    // Convert
        //                    Console.WriteLine("=> Convert music file: {0}", destFile);
        //                    AudioConverter.Instance.ToMP3(srcFile.FullName, destFile);
        //                    result.NbConvertedMusic++;
        //                }
        //            }
        //        }
        //    }

        //    // Recursive directories
        //    var subdirs = src.GetDirectories().ToList();
        //    subdirs.ForEach(x => DoBackup(x, new DirectoryInfo(x.FullName.Replace(src.FullName, dest.FullName)), result));
        //}

    }


    public class Result
    {
        public int NbCopiedMusic { get; set; }
        public int NbConvertedMusic { get; set; }
        public int NbIgnoredMusic { get; set; }
        public int NbTotalMusic { get { return NbCopiedMusic + NbConvertedMusic + NbIgnoredMusic; } }

        public int NbCopiedMisc { get; set; }
        public int NbIgnoredMisc { get; set; }
        public int NbTotalMisc { get { return NbCopiedMisc + NbIgnoredMisc; } }

        public int NbCreatedDir { get; set; }

        public bool Success { get; set; }

        public long Duration { get; set; }

        public Result()
        {
            Success = true;
        }
    }

}
