using System;
using System.Runtime.Serialization;


namespace MusicBackup.Entities
{
    [DataContract]
    public class ItemAudio : Item
    {
        #region Audio info

        [DataMember(Name = "Size")]
        public float Size { get; set; }

        [DataMember(Name = "Compression")]
        public int Compression { get; set; }

        [DataMember(Name = "Format")]
        public string Format { get; set; }

        [DataMember(Name = "Duration")]
        public int Duration { get; set; }

        [DataMember(Name = "Channels")]
        public int Channels { get; set; }

        [DataMember(Name = "SampleRate")]
        public float SampleRate { get; set; }

        [DataMember(Name = "SampleSize")]
        public int SampleSize { get; set; }

        [DataMember(Name = "BitRate")]
        public int BitRate { get; set; }

        [DataMember(Name = "AudioQuality")]
        public string AudioQuality { get; set; }

        #endregion

        #region Main Tags

        [DataMember(Name = "Artist")]
        public String Artist { get; set; }

        [DataMember(Name = "AlbumArtist")]
        public String AlbumArtist { get; set; }

        [DataMember(Name = "Composer")]
        public String Composer { get; set; }

        [DataMember(Name = "Label")]
        public String Label { get; set; }

        [DataMember(Name = "Title")]
        public String Title { get; set; }

        [DataMember(Name = "Album")]
        public String Album { get; set; }

        [DataMember(Name = "Track")]
        public String Track { get; set; }

        [DataMember(Name = "Year")]
        public String Year { get; set; }

        [DataMember(Name = "Genre")]
        public string Genre { get; set; }

        #endregion

        internal ItemAudio(String path, AudioProps props)
        {
            // Audio info
            this.FilePath = path;

            String[] splits;

            this.Size       = props.Get<float>("Size", new string[] { " MB" }, 0, true);
            this.Compression = props.Get<int>("Size", new string[] { "(", "%" }, 1, true);
            this.Format     = props.Get<string>("Type", new string[] { "[.", "]" }, 1);

            var minutes     = props.Get<string>("Length", new string[] { " " }, 0, true);
            var secondes    = props.Get<string>("Length", new string[] { " " }, 2, true);
            this.Duration   =
                    (String.IsNullOrEmpty(minutes) ? 0 : Convert.ToInt32(minutes) * 60)
                  + (String.IsNullOrEmpty(secondes) ? 0 : Convert.ToInt32(secondes));

            this.Channels   = props.Get<int>("Channels"     , new string[] { "(" }      , 0, true);
            this.SampleRate = props.Get<float>("Sample Rate", new string[] { "KHz" }    , 0, true);
            this.SampleSize = props.Get<int>("Sample Size"  , new string[] { "bit" }    , 0, true);
            this.BitRate    = props.Get<int>("Bit Rate"     , new string[] { "kbps" }   , 0, true);

            this.AudioQuality = props["Audio Quality"];

            // Main Tags
            this.Artist = props["Artist"];
            this.AlbumArtist = props["Album Artist"];
            this.Composer = props["Composer"];
            this.Label = props["Label"];
            this.Title = props["Title"];
            this.Album = props["Album"];
            this.Track = props["Track"];
            this.Year = props["Year"];
            this.Genre = props["Genre"];
        }


        public AudioProps GetAllProperties()
        {
            return new AudioProps(FilePath);
        }
    }
}
