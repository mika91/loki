using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using Ionic.Zip;
using Loki.Utils;
using log4net;

namespace MusicBackup.Entities
{
    [DataContract]
    public class AudioLibrary
    {
        private static ILog Log = LogManager.GetLogger(typeof (AudioLibrary));

        [DataMember(Name = "Root", Order = 0)]
        private string _path = "";
        public String Root 
        {
            get { return _path; }
            set
            {
                _path = value;
                Log.Info(() => "Set Library path: {0}", value);
                Log.Info(() => "A new scan is required", value);
                Scan();
            }
        }

        [DataMember(Name = "LastUpdate", Order = 1)]
        public DateTime LastUpdate { get; internal set; }

        private Dictionary<String, Item> _items = new Dictionary<String, Item>();
        [DataMember(Name = "Items", Order = 2)]
        private List<Item> ItemList
        {
            get { return _items.Values.ToList(); }
            set { _items = value.ToDictionary(x => x.FilePath); }
        }

        private AudioLibrary()
        {
        }

        /// <summary>
        /// Create a new library
        /// </summary>
        /// <param name="dirPath">library path</param>
        /// <returns></returns>
        public static AudioLibrary Create(String dirPath)
        {
            var lib = new AudioLibrary();
            lib.Root = dirPath;
            lib.Scan();

            return lib;
        }

        /// <summary>
        /// Update the audio library
        /// </summary>
        /// <param name="force">Force to update already existing entries</param>
        public IEnumerable<ScanResult> Scan(bool force = false)
        {
            Log.Info(() => "Scan AudioLibrary <{0}>", Root);
            LastUpdate = DateTime.Now;

            // #######################
            // List all files on disk
            // #######################
            var filesOnDisk = GetFilesOnDisk(new DirectoryInfo(Root)).ToList();
            Log.Info(() => "{0} files found on disk", filesOnDisk.Count);


            // ##############################
            // Remove deleted files from disk
            // ##############################
            foreach (var item in _items.Values)
            {
                if (!filesOnDisk.Contains(item.FilePath))
                {
                    // Log
                    Log.Info(() => "{0} file deleted: {1}", item is ItemAudio ? "Music" : "Misc", item.FilePath);

                    // yield results
                    yield return new ScanResult() { Action = ScanResult.ActionType.Deleted, Path = item.FilePath };
                }
            }


            // #############################################
            // Add/update library accroding to files on disk
            // #############################################
            foreach (var file in filesOnDisk)
            {
                // Add/update new file
                var newfile = !_items.ContainsKey(file);
                var item = ItemFactory.Create(file);
                _items[file] = item;

                // Log & yield results
                if (newfile)
                {
                    Log.InfoFormat("New {0} file added: {1}", item is ItemAudio ? "Music" : "Misc", item.FilePath);
                    LogProperties(item);
                    yield return new ScanResult() { Action = ScanResult.ActionType.Added, Path = item.FilePath };
                }
                else if (force)
                {
                    Log.InfoFormat("{0} file updated: {1}", item is ItemAudio ? "Music" : "Misc", item.FilePath);
                    LogProperties(item);
                    yield return new ScanResult() { Action = ScanResult.ActionType.Updated, Path = item.FilePath };
                }
                else
                {
                    Log.DebugFormat("{0} file ignored: {1}", item is ItemAudio ? "Music" : "Misc", item.FilePath);
                    yield return new ScanResult() { Action = ScanResult.ActionType.Ignored, Path = item.FilePath };
                }
            }
        }


        #region Items accessors

        public Item this[string path]
        {
            get
            {
                Item item;
                return _items.TryGetValue(path, out item)
                           ? item
                           : null;
            }
        }

        public IEnumerable<Item> GetAudioFiles()
        {
            return _items.Values.Where(x => x is ItemAudio).OrderBy(x => x.FilePath);
        }

        public IEnumerable<Item> GetMiscFiles()
        {
            return _items.Values.Where(x => x is ItemMisc).OrderBy(x => x.FilePath);
        }

        public IEnumerable<Item> GetAllFiles()
        {
            return _items.Values.OrderBy(x => x.FilePath);
        }

        #endregion
     
        #region Import/Export

        public bool Save(String path)
        {
            try
            {
                using (var zip = new ZipFile(path))
                {
                    zip.UpdateEntry("AudioLibrary.xml", (name, stream) =>
                    {
                        using (var w = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true }))
                        {
                            var ds = new DataContractSerializer(typeof(AudioLibrary));
                            ds.WriteObject(w, this);
                            stream.Flush();
                        }
                    });
                    zip.Save();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while loading library: {0}", ex.Message);
                return false;
            }
          
        }

        public static AudioLibrary Load(String path)
        {
            try
            {
                using (var zip = ZipFile.Read(path))
                using (var mem = new MemoryStream())
                {
                    var entry = zip.Entries.FirstOrDefault(x => x.FileName == "AudioLibrary.xml");
                    if (entry != null)
                    {
                        entry.Extract(mem);
                        mem.Position = 0;

                        var ds = new DataContractSerializer(typeof(AudioLibrary));
                        return (AudioLibrary)ds.ReadObject(mem);
                    }

                    Console.WriteLine("No library found");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while loading library: {0}", ex.Message);
                return null;
            }
        }

        #endregion

        #region Misc functions

        /// <summary>
        /// List all files on a directory
        /// </summary>
        /// <param name="dir">Directory to check</param>
        /// <returns>List of files path</returns>
        internal static IEnumerable<string> GetFilesOnDisk(DirectoryInfo dir)
        {
            // Check directory
            if (!dir.Exists)
            {
                Log.Warn(() => "Directory {0} doesn't exist", dir.FullName);
                yield break;
            }

            // List files
            foreach (var file in dir.GetFiles())
                yield return file.FullName;

            // Recursively list sub-folder files
            foreach (var subdir in dir.GetDirectories())
                foreach (var file in GetFilesOnDisk(subdir))
                    yield return file;
        }

        /// <summary>
        /// Log Audio files properties
        /// </summary>
        /// <param name="item"></param>
        private void LogProperties(Item item)
        {
            if (!Log.IsDebugEnabled)
                return;

            var aitem = item as ItemAudio;
            if (aitem == null)
                return;

            Log.Debug("{");
            (item as ItemAudio).GetAllProperties()
                               .ToList()
                               .ForEach(x => Log.DebugFormat("\t{0}: {1}", x.Key, x.Value));
            Log.Debug("}");
        }


        #endregion

    }




   

   
}
