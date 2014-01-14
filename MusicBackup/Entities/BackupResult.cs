using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace MusicBackup.Entities
{
    [DataContract]
    public class BackupResult
    {
        [DataMember]
        public string InPath    { get; internal set; }

        [DataMember]
        public string OutPath { get; internal set; }

        [DataMember]
        public ActionType Action { get; internal set; }

        [DataContract]
        public enum ActionType
        {
            Ignored = 0, Copied = 1, Converted = 2
        }

    }






}
