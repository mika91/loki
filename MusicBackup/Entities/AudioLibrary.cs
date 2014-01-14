using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using Ionic.Zip;
using Loki.Utils;
using Loki.Utils.Extensions;
using MusicBackup.dMC;
using log4net;

namespace MusicBackup.Entities
{
    [DataContract]
    public class AudioLibrary
    {
        private static ILog Log = LogManager.GetLogger(typeof(AudioLibrary));

        [DataMember(Name = "Root", Order = 0)]
        public String Root { get; private set; }

        [DataMember(Name = "LastUpdate", Order = 1)]
        public DateTime LastUpdate { get; internal set; }

        Dictionary<String, LibItem> _dicoItems = new Dictionary<String, LibItem>();
        [DataMember(Name = "Items", Order = 2)]
        private List<LibItem> ItemList
        {
            get { return _dicoItems.Values.OrderBy(x=>x.FilePath).ToList(); }
            set { _dicoItems = value.ToDictionary(x => x.FilePath); }
        }

        private AudioLibrary()
        {
        }

        /// <summary>
        /// Create a new library
        /// </summary>
        /// <param name="dirPath">library path</param>
        /// <param name="lib"></param>
        /// <returns></returns>
        public static List<ScanResult> Create(String dirPath, out AudioLibrary lib)
        {
            lib = new AudioLibrary() {Root = dirPath};
            return lib.Scan(true);
        }

        /// <summary>
        /// Update the audio library
        /// </summary>
        /// <param name="force">Force to update already existing entries</param>
        public List<ScanResult> Scan(bool force = false)
        {
            return scan(force).ToList();
        }

        private IEnumerable<ScanResult> scan(bool force = false)
        {
            Log.Info(() => "Scan AudioLibrary <{0}> - Forcemode = {1}", Root, force);
            LastUpdate = DateTime.Now;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            // #######################
            // List all files on disk
            // #######################
            var filesOnDisk =new HashSet<string>(
                new DirectoryInfo(Root).GetFiles("*", SearchOption.AllDirectories)
                                       .Select(x => x.FullName));
 
            Log.Info(() => "{0} files found on disk", filesOnDisk.Count);


            // ##############################
            // Remove deleted files from disk
            // ##############################
            List<String> removed = new List<String>();
            foreach (var item in _dicoItems.Values)
            {
                if (!filesOnDisk.Contains(item.FilePath))
                {
                    // Log
                    Log.Info(() => "{0} file deleted: {1}", (item.AudioInfo != null) ? "Music" : "Misc", item.FilePath);

                    // Mark as removed file
                    removed.Add(item.FilePath);

                    // yield results
                    yield return new ScanResult() { Action = ScanResult.ActionType.Deleted, Path = item.FilePath };
                }
            }
            removed.ForEach(x=> _dicoItems.Remove(x));

            // #############################################
            // Add/update library according to files on disk
            // #############################################
            foreach (var file in filesOnDisk)
            {
                // Add/update new file
                LibItem item = null;
                bool newfile = !_dicoItems.TryGetValue(file, out item);

                // Log & yield results
                if (newfile)
                {
                    item = new LibItem(file);
                    _dicoItems[file] = item;
                    Log.Info(()=>"New {0} file added: {1}", (item.AudioInfo != null) ? "Music" : "Misc", item.FilePath);
                    LogProperties(item);
                    yield return new ScanResult() { Action = ScanResult.ActionType.Added, Path = item.FilePath };
                }
                else if (force)
                {
                    item = new LibItem(file);
                    Log.Info(() => "{0} file updated: {1}", (item.AudioInfo != null) ? "Music" : "Misc", item.FilePath);
                    LogProperties(item);
                    _dicoItems[file] = item;
                    yield return new ScanResult() { Action = ScanResult.ActionType.Updated, Path = item.FilePath };
                }
                else
                {
                    Log.Debug(() => "{0} file ignored: {1}", (item.AudioInfo != null) ? "Music" : "Misc", item.FilePath);
                    yield return new ScanResult() { Action = ScanResult.ActionType.Ignored, Path = item.FilePath };
                }
            }

            sw.Stop();
            Log.Info(()=> "Scan library done in {0}ms", sw.ElapsedMilliseconds);
        }


        #region Items accessors

        public LibItem this[string path]
        {
            get
            {
                LibItem item;
                return _dicoItems.TryGetValue(path, out item)
                           ? item
                           : null;
            }
        }

        public IEnumerable<LibItem> GetAudioItems()
        {
            return _dicoItems.Values.Where(x => x.AudioInfo != null).OrderBy(x => x.FilePath);
        }

        public IEnumerable<LibItem> GetMiscItems()
        {
            return _dicoItems.Values.Where(x => x.AudioInfo == null).OrderBy(x => x.FilePath);
        }

        public IEnumerable<LibItem> GetAllItems()
        {
            return _dicoItems.Values.OrderBy(x => x.FilePath);
        }

        internal void Add(string filepath)
        {
            // TOD safer
            _dicoItems.Add(filepath, new LibItem(filepath));
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
        /// Log Audio files properties
        /// </summary>
        /// <param name="item"></param>
        private void LogProperties(LibItem item)
        {
            if (!Log.IsDebugEnabled)
                return;

            var props = dMCProps.Get(item.FilePath);

            if (props == null)
                return;

            Log.Debug("{");
            props.ForEach(x => Log.DebugFormat("\t{0}: {1}", x.Key, x.Value));
            Log.Debug("}");
        }


        #endregion

    }

    [DataContract]
    public class LibItem
    {
        [DataMember]
        public String    FilePath   { get; private set; }


        [DataMember]
        public AudioInfo AudioInfo  { get; private set; }

        public String Extension {
            get { return new FileInfo(FilePath).Extension; }
        }

        /// <summary>
        /// For serilization only
        /// </summary>
        private LibItem()
        {
        }

        public LibItem(String path)
        {
            FilePath = path;
            AudioInfo = AudioInfo.Get(path);
        }

    }







}
