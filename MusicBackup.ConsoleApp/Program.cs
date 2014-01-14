using System;
using System.IO;
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
            // Read configuration
            var src_path = new DirectoryInfo(Properties.Settings.Default.Source);
            var dest_path = new DirectoryInfo(Properties.Settings.Default.Destination);
            var nbCores = Convert.ToInt32(Properties.Settings.Default.NbCores);

            // Init converter
            AudioConverter.Instance.NbCores = 3;    // nbCores;

            // Import source library
            Log.Info(() => "Trying to load source libray...");
            var srcLib = AudioLibrary.Load("src.content");
            if (srcLib == null)
            {
                Log.Info(() => "No libray found. Create a new one");
                srcLib = AudioLibrary.Create(src_path.FullName);
            }

            // Import backup library
            Log.Info(() => "Trying to load backup libray...");
            var destLib = AudioLibrary.Load("dest.content");
            if (destLib == null)
            {
                Log.Info(() => "No libray found. Create a new one");
                destLib = AudioLibrary.Create(dest_path.FullName);
            }

            //// Comparing both libraries
            //AudioManager.Backup(srcLib, destLib, false, true);

            // Saving Libraries
            srcLib.Save("src.content");
            destLib.Save("dest.content");

            // Exit
            Console.WriteLine("Press a key to exit..");
            Console.ReadLine();
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
        //    var srcFiles  = src.GetFiles().OrderBy(x => x.Name).ToList();
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
        //           if (!isMusicFile)
        //           {
        //               // Simple copy
        //               Console.WriteLine("=> Copy misc file: {0}", destFile);
        //               File.Copy(srcFile.FullName, destFile);
        //               result.NbCopiedMisc++;
        //           }
        //           else
        //           {
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
        //    subdirs.ForEach( x => DoBackup(x, new DirectoryInfo(x.FullName.Replace(src.FullName, dest.FullName)), result));
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
