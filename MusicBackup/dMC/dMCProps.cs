using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DMCSCRIPTINGLib;
using log4net;
using Loki.Utils;

namespace MusicBackup.dMC
{
    /// <summary>
    /// dBpoweramp audio files properties collection.
    /// </summary>
    public class dMCProps : IEnumerable<KeyValuePair<String, String>>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (dMCProps));

        private static readonly Converter converter = new Converter();
        private readonly Dictionary<string, string> _dico;

        public static dMCProps Get(String path)
        {
            return new dMCProps(path);
        }

        protected dMCProps(String path)
        {
            _dico = new Dictionary<string, string>();

            // check file exists
            if (!File.Exists(path))
                return;

            // Read dBpoweramp audio properties
            var props = new List<string>();
            try
            {   
                props = 
                    converter.AudioProperties[path]
                             .Split(new string[] { "\r" }, StringSplitOptions.RemoveEmptyEntries)
                             .ToList();

            }
            catch (Exception ex)
            {
                Log.Error(() => "Error while reading dBpoweramp properties: {0}", ex.Message);
            }
                 
            // Store each property in the dictionary
            props.ForEach(x =>
            {
                try
                {
                    var splits = x.Split(new string[] { " :" }, StringSplitOptions.RemoveEmptyEntries);
                    if (splits.Count() == 2)
                    {
                        var name = splits[0];

                        var pos = splits[1].IndexOf('\t');
                        pos = pos == -1 ? 1 : pos + 1;
                        var info = splits[1].Substring(pos);

                        if (_dico.ContainsKey(name))
                            Log.Warn(() => "dMCProps already has a property named {0}. Ignore it (value={1})", name, info);
                        else
                            _dico.Add(name, info);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(() => "Error while parsing dBpoweramp property <{0}>: {1}", x, ex.Message);
                }
            });   
        }

        public String this[string name]
        {
            get
            {
                String val;
                return _dico.TryGetValue(name, out val)
                           ? val
                           : String.Empty;
            }
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _dico.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dico.GetEnumerator();
        }
    }   

}
