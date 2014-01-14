using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DMCSCRIPTINGLib;

namespace MusicBackup.Entities
{
    public static class ItemFactory
    {
        private static Converter converter = new Converter();

        public static Item Create(String path)
        {
            var file = new FileInfo(path);
            if (!file.Exists)
            {
                Console.WriteLine("File {0} doesn't exist", path);
                return null;
            }

            var audioProps = new AudioProps(file.FullName);
            return audioProps.Any()
                       ? (Item)new ItemAudio(file.FullName, audioProps)
                       : (Item)new ItemMisc(file.FullName);
        }
    }
}
