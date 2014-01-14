using System;
using System.Runtime.Serialization;

namespace MusicBackup.Entities
{
    [DataContract]
    public class ItemMisc : Item
    {
        internal ItemMisc(String path)
        {
            FilePath = path;
        }
    }
}
