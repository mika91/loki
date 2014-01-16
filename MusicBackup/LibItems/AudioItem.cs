using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using MusicBackup.dMC;
using log4net;
using Loki.Utils;

namespace MusicBackup
{
    [DataContract]
    public class AudioItem : Item
    {
        public static readonly ILog Log = LogManager.GetLogger(typeof(AudioItem));

        #region Properties

        // ********************
        //   Audio Properties
        // ********************

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


        // ********************
        //       Metadata
        // ********************

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

        public AudioItem(String path, dMCProps props)
            : base(path)
        {
            try
            {
                //String[] splits;

                //this.Size = props.Get<float>("Size", new string[] { " MB" }, 0, true);
                this.Size = ParseSize(props);
                //this.Compression = props.Get<int>("Size", new string[] { "(", "%" }, 1, true);
                this.Compression = Parse<int>(props, "Size", new string[] { "(", "%" }, 1, true);
                //this.Format = props.Get<string>("Type", new string[] { "[.", "]" }, 1);
                this.Format = Parse<string>(props, "Type", new string[] { "[.", "]" }, 1);
                //var minutes = props.Get<string>("Length", new string[] { " " }, 0, true);
                //var secondes = props.Get<string>("Length", new string[] { " " }, 2, true);
                //this.Duration = (String.IsNullOrEmpty(minutes) ? 0 : Convert.ToInt32(minutes) * 60)
                //                    + (String.IsNullOrEmpty(secondes) ? 0 : Convert.ToInt32(secondes));
                this.Duration = ParseDuration(props);

                //this.Channels = props.Get<int>("Channels", new string[] { "(" }, 0, true);
                this.Channels = Parse<int>(props, "Channels", new string[] { "(" }, 0, true);
                //this.SampleRate = props.Get<float>("Sample Rate", new string[] { "KHz" }, 0, true);
                this.SampleRate = Parse<float>(props, "Sample Rate", new string[] { "KHz" }, 0, true);
                //this.SampleSize = props.Get<int>("Sample Size", new string[] { "bit" }, 0, true);
                this.SampleSize = Parse<int>(props, "Sample Size", new string[] { "bit" }, 0, true);
                //this.BitRate = props.Get<int>("Bit Rate", new string[] { "kbps" }, 0, true);
                this.BitRate = Parse<int>(props, "Bit Rate", new string[] { "kbps" }, 0, true);

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
            catch (Exception ex)
            {
                Log.Error(() => "Error while parsing dBPoweramp properties: {0}", ex.Message);
            }
        }

        // Only for serializtion purpose
        private AudioItem() { }

        #region Parse methods

        int ParseSize(dMCProps props)
        {
            // Read property value
            String val = props["Size"];

            // If not defined, return default value
            if (String.IsNullOrEmpty(val))
            {
                Log.Warn(() => "No <Size> property found");
                return -1;
            }

            try
            {
                // Parse file size
                var words = val.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < words.Length; i++)
                {
                    if (words[i].Contains("GB"))
                        return (int)(Convert.ToSingle(words[i - 1]) * 1024 * 1204 * 1204);

                    if (words[i].Contains("MB"))
                        return (int)(Convert.ToSingle(words[i - 1]) * 1024 * 1204);

                    if (words[i].Contains("KB"))
                        return (int)(Convert.ToSingle(words[i - 1]) * 1024);

                    if (words[i].Contains("KB"))
                        return (int)(Convert.ToSingle(words[i - 1]));
                }

                return -1;
            }
            catch (Exception ex)
            {
                Log.Error(() => "Error while parsing Size <{0}>: {1}", ex.Message);
                return -2;
            }

        }

        int ParseDuration(dMCProps props)
        {
            // Read property value
            String val = props["Length"];

            // If not defined, return default value
            if (String.IsNullOrEmpty(val))
            {
                Log.Warn(() => "No <Length> property found");
                return 0;
            }

            try
            {
                // Parse file duration
                float duration = 0;
                var words = val.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < words.Length; i++)
                {
                    if (words[i].Contains("hou"))
                        duration += 3600 * Convert.ToSingle(words[i - 1]);

                    if (words[i].Contains("min"))
                        duration += 60 * Convert.ToSingle(words[i - 1]);

                    if (words[i].Contains("sec"))
                        duration += Convert.ToSingle(words[i - 1]);
                }

                return (int)duration;
            }
            catch (Exception ex)
            {
                Log.Error(() => "Error while parsing Duration <{0}>: {1}", val, ex.Message);
                return -2;
            }
        }

        T Parse<T>(dMCProps props, string name, String[] separators, int index, bool removeWhite = false)
        {
            // Read property value
            String prop = props[name];

            // If not defined, return default value
            if (String.IsNullOrEmpty(prop))
            {
                Log.Warn(() => "No <{0}> property found", name);
                return default(T);
            }

            // Extract wanted value
            var val = Extract(prop, separators, index, removeWhite);
            if (String.IsNullOrEmpty(val))
            {
                Log.Error(() => "No substring candidate found for propertry <{0}><{1}>", name, prop);
                return default(T);
            }

            // Convert to desired type
            try
            {
                var result = (T)Convert.ChangeType(val, typeof(T));
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(() => "Error while converting propertry <{0}><{1}>: {2}", name, val, ex.Message);
                return default(T);
            }
        }

        String Extract(String prop, String[] separators, int index, bool removeWhite = false)
        {
            var splits = prop.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            return (index < splits.Count())
                ? removeWhite
                    ? Regex.Replace(splits[index], @"\p{Z}", "")
                    : splits[index]
                : String.Empty;
        }

        #endregion
    }
}
