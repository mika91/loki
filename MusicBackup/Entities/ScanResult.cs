using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace MusicBackup.Entities
{
    [DataContract]
    public class ScanResult
    {
        [DataMember]
        public string   Path { get; set; }

        [DataMember]
        public ActionType Action { get; set; }

        [DataContract]
        public enum ActionType
        {
            Ignored = 0, Added = 1, Deleted = 2, Updated = 3
        }
    }
}
