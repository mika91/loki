using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DMCSCRIPTINGLib;

namespace MusicBackup.Entities
{
    public class AudioProps : IEnumerable<KeyValuePair<String, String>>
    {
        private static Converter converter = new Converter();
        private Dictionary<string, string> dico;

        public AudioProps(String path)
        {
            dico = new Dictionary<string, string>();

            // Read DBPowerAmp audio properties
            var props =
                converter.AudioProperties[path].Split(new string[] { "\r" }, StringSplitOptions.RemoveEmptyEntries)
                                               .ToList();
            props.ForEach(x =>
            {
                var splits = x.Split(new string[] { " :" }, StringSplitOptions.RemoveEmptyEntries);
                if (splits.Count() == 2)
                {
                    var name = splits[0];

                    var pos = splits[1].IndexOf('\t');
                    pos = pos == -1 ? 1 : pos + 1;
                    var info = splits[1].Substring(pos);

                    dico.Add(name, info);
                }
            });
        }

        public String this[string name]
        {
            get
            {
                String val;
                return dico.TryGetValue(name, out val)
                           ? val
                           : String.Empty;
            }
        }

        public String this[String name, String[] separators, int index, bool removeWhite = false]
        {
            get
            {
                String val;
                if (dico.TryGetValue(name, out val))
                {
                    var splits = val.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                    return (index < splits.Count())
                               ? removeWhite
                                   ? Regex.Replace(splits[index], @"\p{Z}", "")
                                   : splits[index]
                               : String.Empty;
                }

                return String.Empty;
            }
        }


        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return dico.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return dico.GetEnumerator();
        }


        public T Get<T>(String name, String[] separators, int index, bool removeWhite = false)
        {
            String val;
            if (dico.TryGetValue(name, out val))
            {
                var splits = val.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                val = index < splits.Count()
                           ? removeWhite
                               ? Regex.Replace(splits[index], @"\p{Z}", "")
                               : splits[index]
                           : String.Empty;

                try
                {
                    var result =  (T)Convert.ChangeType(val, typeof(T));
                    return result;
                }
                catch (Exception ex)
                {
                    return default(T);
                }    
            }

            return default(T);
        }
    }   

}
