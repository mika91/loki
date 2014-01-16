using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            // Import source library
            Log.Info(() => "Trying to load source libray...");
            var srcLib = AudioLibrary.Load("src.content");
            if (srcLib == null)
            {
                Log.Info(() => "No library found. Create a new one");
                srcLib = AudioLibrary.Create(src_path.FullName);
            }
            else if (new DirectoryInfo(srcLib.Root).FullName != src_path.FullName) // TODO securise if path doen't exist
            {
                Log.Info(() => "Library root folder doesn't match. Create a new one");
                srcLib = AudioLibrary.Create(src_path.FullName);
            }
          
            //PrintScanRes(scanResults);

            // Import libraries
            Log.Info(() => "Trying to load backup libray...");
            var destLib = AudioLibrary.Load("dest.content");
            if (destLib == null)
            {
                Log.Info(() => "No library found. Create a new one");
                destLib = AudioLibrary.Create(dest_path.FullName);
            }
            else if (new DirectoryInfo(destLib.Root).FullName != dest_path.FullName) // TODO securise if path doen't exist
            {
                Log.Info(() => "Library root folder doesn't match. Create a new one");
                destLib = AudioLibrary.Create(dest_path.FullName);
            }

            //PrintScanRes(scanResults);

            // Backing up library
            var results = AudioManager.Backup(srcLib, destLib, false, false).ToList();
            PrintBackupRes(results);

            // Saving Libraries
            Log.Info(() => "Saving source library...");
            srcLib.Save("src.content");
            Log.Info(() => "Saving destination library...");
            destLib.Save("dest.content");

            //// Exit
            //Console.WriteLine("Press a key to exit..");
            //Console.ReadLine();
        }

        public static void PrintBackupRes(List<AudioManager.BackupResult> results)
        {
            Log.Info(() => "Backup summary:");

            var audioFiles  = results.Where(x => x.Source is AudioItem).ToList();
            var miscFiles   = results.Where(x => !(x.Source is AudioItem)).ToList();
           
            Log.Info(() => "\tMusic files converted : {0}", audioFiles.Count(x => x.Action == AudioManager.BackupResult.ActionType.Converted));
            Log.Info(() => "\tMusic files copied    : {0}", audioFiles.Count(x => x.Action == AudioManager.BackupResult.ActionType.Copied));
            Log.Info(() => "\tMusic files ignored   : {0}", audioFiles.Count(x => x.Action == AudioManager.BackupResult.ActionType.Ignored));
            Log.Info(() => "\tMusic files total     : {0}", audioFiles.Count());
                                                 
            Log.Info(() => "");               
            Log.Info(() => "\tMisc files copied     : {0}" , miscFiles.Count(x => x.Action == AudioManager.BackupResult.ActionType.Copied));
            Log.Info(() => "\tMisc files ignored    : {0}" , miscFiles.Count(x => x.Action == AudioManager.BackupResult.ActionType.Ignored));
            Log.Info(() => "\tMisc files total      : {0}" , miscFiles.Count());
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
