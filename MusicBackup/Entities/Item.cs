using System;
using System.IO;
using System.Runtime.Serialization;

namespace MusicBackup.Entities
{
    [DataContract]
    [KnownType(typeof(ItemAudio))]
    [KnownType(typeof(ItemMisc))]
    public class Item
    {
        [DataMember(Name = "FilePath")]
        public String FilePath { get; set; }

        public String Extension
        {
            get { return Path.GetExtension(FilePath); }
        }
    }

  

}
