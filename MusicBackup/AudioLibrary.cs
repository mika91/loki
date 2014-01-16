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

namespace MusicBackup
{
    [DataContract]
    public class AudioLibrary
    {
        private static ILog Log = LogManager.GetLogger(typeof(AudioLibrary));

        [DataMember(Name = "Root", Order = 0)]
        public String Root { get; private set; }

        [DataMember(Name = "LastUpdate", Order = 1)]
        public DateTime LastUpdate { get; internal set; }

        Dictionary<String, Item> _dicoItems = new Dictionary<String, Item>();
        [DataMember(Name = "Items", Order = 2)]
        private List<Item> ItemList
        {
            get { return _dicoItems.Values.OrderBy(x=>x.Path).ToList(); }
            set { _dicoItems = value.ToDictionary(x => x.Path); }
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
            return new AudioLibrary() {Root = dirPath};
        }

        /// <summary>
        /// Update the audio library
        /// </summary>
        /// <param name="force">Force to update already existing items</param>
        public ScanResult Scan(bool force = false)
        {
            Log.Info(() => "Scan AudioLibrary <{0}>", Root);
            Log.Info(() => "\tForceMode: {0}", force);
 
            var result = new ScanResult() {StartTime = DateTime.Now};

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
            var missings = new List<String>();
            foreach (var item in _dicoItems.Values)
            {
                if (!filesOnDisk.Contains(item.Path))
                {
                    Log.Info(() => "{0} file deleted: {1}", (item is AudioItem) ? "Music" : "Misc", item.Path);
                    missings.Add(item.Path);
                    
                    FireItemDeleted(item); 
                    result.Deleted.Add(item);
                }
            }
            missings.ForEach(x=> _dicoItems.Remove(x));

            // #############################################
            // Add/update library according to files on disk
            // #############################################
            foreach (var file in filesOnDisk)
            {
                // Add/update new file
                Item item = null;
                bool newfile = !_dicoItems.TryGetValue(file, out item);

                // Log & yield results
                if (newfile)
                {
                    item = ItemFactory.Create(file);
                    _dicoItems[file] = item;
                    Log.Info(()=>"New {0} file added: {1}", (item is AudioItem) ? "Music" : "Misc", item.Path);
                    LogProperties(item);

                    FireItemAdded(item);
                    result.Added.Add(item);
                }
                else if (force)
                {
                    item = ItemFactory.Create(file);
                    Log.Info(() => "{0} file updated: {1}", (item is AudioItem) ? "Music" : "Misc", item.Path);
                    LogProperties(item);
                    _dicoItems[file] = item;

                    FireItemUpdated(item);
                    result.Updated.Add(item);
                }
                else
                {
                    Log.Debug(() => "{0} file ignored: {1}", (item is AudioItem) ? "Music" : "Misc", item.Path);
                    
                    FireItemIgnored(item);
                    result.Ignored.Add(item);
                }
            }

            LastUpdate = DateTime.Now;
            result.StopTime = DateTime.Now;
            LogScanResult(result);

            return result;
        }


        #region Items accessors

        public Item this[string path]
        {
            get
            {
                Item item;
                return _dicoItems.TryGetValue(path, out item)
                           ? item
                           : null;
            }
        }

        public IEnumerable<Item> GetAudioItems()
        {
            return _dicoItems.Values.Where(x => x is AudioItem).OrderBy(x => x.Path);
        }

        public IEnumerable<Item> GetMiscItems()
        {
            return _dicoItems.Values.Where(x => !(x is AudioItem)).OrderBy(x => x.Path);
        }

        public IEnumerable<Item> GetAllItems()
        {
            return _dicoItems.Values.OrderBy(x => x.Path);
        }

        #endregion

        #region Scan Events

        public event Action<Item> ItemAdded;
        public event Action<Item> ItemDeleted;
        public event Action<Item> ItemUpdated;
        public event Action<Item> ItemIgnored;

        private void FireItemAdded(Item item)
        {
            if (ItemAdded != null)
                ItemAdded(item);
        }

        private void FireItemDeleted(Item item)
        {
            if (ItemDeleted != null)
                ItemDeleted(item);
        }

        private void FireItemUpdated(Item item)
        {
            if (ItemUpdated != null)
                ItemUpdated(item);
        }

        private void FireItemIgnored(Item item)
        {
            if (ItemIgnored != null)
                ItemIgnored(item);
        }

        #endregion

        #region ScanResults

        [DataContract]
        public class ScanResult
        {
            [DataMember]
            public DateTime StartTime { get; set; }
            [DataMember]
            public DateTime StopTime { get; set; }

            [DataMember]
            public List<Item> Added { get; private set; }
            [DataMember]
            public List<Item> Deleted { get; private set; }
            [DataMember]
            public List<Item> Ignored { get; private set; }
            [DataMember]
            public List<Item> Updated { get; private set; }

            public IEnumerable<Item> All { get { return Added.Concat(Deleted.Concat(Ignored.Concat(Updated))); } }

            public ScanResult()
            {
                Added = new List<Item>();
                Deleted = new List<Item>();
                Ignored = new List<Item>();
                Updated = new List<Item>();
            }
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
                Log.Fatal(()=>"Error while loading library: {0}", ex.Message);
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

                    Log.Fatal(()=>"No library found");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(()=>"Error while loading library: {0}", ex.Message);
                return null;
            }
        }

        #endregion

        #region Misc functions

        /// <summary>
        /// Log Audio files properties
        /// </summary>
        /// <param name="item"></param>
        private void LogProperties(Item item)
        {
            if (!Log.IsDebugEnabled)
                return;

            var props = dMCProps.Get(item.Path);

            if (props == null)
                return;

            Log.Debug("{");
            props.ForEach(x => Log.DebugFormat("\t{0}: {1}", x.Key, x.Value));
            Log.Debug("}");
        }

        public static void LogScanResult(ScanResult res)
        {
            Log.Info(() => "Scan finished in {0} ms", (res.StopTime - res.StartTime).TotalMilliseconds);
            Log.Info(() => "\tFiles added   : {0}", res.Added.Count);
            Log.Info(() => "\tFiles deleted : {0}", res.Deleted.Count);
            Log.Info(() => "\tFiles ignored : {0}", res.Ignored.Count);
            Log.Info(() => "\tFiles updated : {0}", res.Updated.Count);
            Log.Info(() => "\tFiles total   : {0}", res.All.Count());
        }

        #endregion

    }



}
