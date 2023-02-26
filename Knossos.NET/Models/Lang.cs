using System.Collections.Generic;

namespace Knossos.NET.Models
{
    /*
        Used to handle string localization.
        Loads and handles the lang files json.
    */
    public static class Lang
    {
        private struct Language
        {
            public string name { get; set; }
            public string version { get; set; }
            public List<LangString> strings { get; set;}
        }

        private struct LangString
        {
            public string key { get; set; }
            public string value { get; set; }
        }

        private static List<Language> installedLangs = new List<Language>();
        private static int enabledLangIndex = 0;

        public static void LoadFiles()
        {
            //test
            var test = new Language();
            test.name = "English";
            test.version = "1.0.0";
            test.strings = new List<LangString>();
            test.strings.Add(new LangString { key = "test", value = "it works!" });
            installedLangs.Add(test);
            enabledLangIndex = 0;
        }

        public static string GetString(string key)
        {
            if(enabledLangIndex +1 <= installedLangs.Count)
            {
                var lang = installedLangs[enabledLangIndex];
                foreach(var langString in lang.strings)
                {
                    if(langString.key == key)
                    {
                        return langString.value;
                    }
                }
                return key;
            }
            else
            {
                return key;
            }
        }
    }
}
