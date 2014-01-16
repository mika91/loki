using System;
using System.Runtime.Serialization;
using System.IO;
using System.Linq;
using log4net;
using Loki.Utils;

namespace MusicBackup
{
    [DataContract]
    [KnownType(typeof(AudioItem))]
    [KnownType(typeof(MiscItem))]
    public class Item
    {
        [DataMember]
        public string Path { get; private set; }

        public String Extension
        {
            get { return new FileInfo(Path).Extension; }
        }

        protected Item(String path)
        {
            Path = path;
        }

        // For serialization
        protected Item()
        {
        }
    }

    public class ItemFactory
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ItemFactory)); 

        public static Item Create(string filepath)
        {
            // Check file exists
            if (!File.Exists(filepath))
            {
                Log.Error(()=>"Impossible to create a library item. File {0} doesn't exist.", filepath);
                return null;
            }

            // Try getting dBpoweramp properties
            var aprops = dMC.dMCProps.Get(filepath);

            return (aprops == null || !aprops.Any())
                       ? (Item) new MiscItem(filepath)
                       : (Item) new AudioItem(filepath, aprops); 
          
        }
    }
}
