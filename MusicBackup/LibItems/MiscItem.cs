using System.Runtime.Serialization;

namespace MusicBackup
{
    [DataContract]
    public class MiscItem : Item
    {
        public MiscItem(string path) 
            : base(path)
        {
        }

        // Only for serialization
        private MiscItem(){}
    }
}
