using System.Runtime.Serialization;

namespace MusicBackup.Entities
{
    [DataContract]
    public class BackupResult
    {
        [DataMember]
        public LibItem Source     { get; internal set; }

        [DataMember]
        public LibItem Dest       { get; internal set; }

        [DataMember]
        public ActionType Action { get; internal set; }

        [DataContract]
        public enum ActionType
        {
            Ignored = 0, Copied = 1, Converted = 2
        }

    }






}
