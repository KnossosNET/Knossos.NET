using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Knossos.NET.Models
{
    /// <summary>
    /// Multicfg file is used to configure standalone servers
    /// </summary>
    public class MultiCfg
    {
        public string? Name { get; set; }
        public string? Password { get; set; }
        public bool UsePXO { get; set; } = false;
        public string? PXOChannel { get; set; }
        public List<string> Ban { get; set; } = new List<string>();
        public bool NoVoice { get; set; } = false;
        public int Port { get; set; } = 0;
        public List<string> Others { get; set; } = new List<string>();

        /// <summary>
        /// Load data from file
        /// Data is saved in {modfolder}\data\multi.cfg
        /// </summary>
        /// <param name="mod"></param>
        /// Returns true/false if successfull
        public bool LoadData(Mod mod)
        {
            UsePXO = false;
            NoVoice = false;
            Ban.Clear();
            Others.Clear();
            Name = null;
            Password = null;
            Port = 0;
            PXOChannel = null;
            try
            {
                if (File.Exists(Path.Combine(mod.fullPath,"data","multi.cfg")))
                {
                    using (StreamReader reader = new StreamReader(Path.Combine(mod.fullPath, "data", "multi.cfg")))
                    {
                        string? line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            var option = "+name";
                            if (line.ToLower().Contains(option+" "))
                            {
                                Name = line.ToLower().Replace(option + " ", "");
                                continue;
                            }
                            option = "+passwd";
                            if (line.ToLower().Contains(option + " "))
                            {
                                Password = line.ToLower().Replace(option + " ", "");
                                continue;
                            }
                            option = "+pxo";
                            if (line.ToLower().Contains(option + " "))
                            {
                                UsePXO = true;
                                try
                                {
                                    PXOChannel = line.Split(" ")[1];
                                }
                                catch { }
                                continue;
                            }
                            option = "+ban";
                            if (line.ToLower().Contains(option + " "))
                            {
                                Ban.Add(line.ToLower().Replace(option + " ", ""));
                                continue;
                            }
                            option = "+no_voice";
                            if (line.ToLower().Contains(option + " "))
                            {
                                NoVoice = true;
                                continue;
                            }
                            option = "+port";
                            if (line.ToLower().Contains(option + " "))
                            {
                                try
                                {
                                    Port = int.Parse(line.ToLower().Replace(option + " ", ""));
                                }
                                catch { }
                                continue;
                            }
                            Others.Add(line.ToLower());
                        }
                    }
                    return true;
                }
                else
                {
                    Log.Add(Log.LogSeverity.Information, "MultiCfg.LoadData()", mod + " does not have a Multi.cfg file.");
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "MultiCfg.LoadData()", ex);
            }
            return false;
        }

        /// <summary>
        /// Save data to file
        /// Data is saved in {modfolder}\data\multi.cfg
        /// </summary>
        /// <param name="mod"></param>
        public void SaveData(Mod mod)
        {
            try
            {
                if(!Directory.Exists(Path.Combine(mod.fullPath, "data")))
                {
                    Directory.CreateDirectory(Path.Combine(mod.fullPath, "data"));
                }
                using (StreamWriter sw = new StreamWriter(Path.Combine(mod.fullPath, "data", "multi.cfg"), false, new UTF8Encoding(false)))
                {
                    if (Others.IndexOf("+name") == -1 && Name != null && Name != string.Empty)
                    {
                        sw.WriteLine("+name " + Name);
                    }
                    if (Others.IndexOf("+passwd") == -1 && Password != null && Password != string.Empty)
                    {
                        sw.WriteLine("+passwd " + Password);
                    }
                    if (Others.IndexOf("+pxo") == -1 && UsePXO)
                    {
                        sw.WriteLine("+pxo " + PXOChannel);
                    }
                    foreach (string b in Ban)
                    {
                        if (Others.IndexOf("+ban " + b) == -1)
                            sw.WriteLine("+ban " + b);
                    }
                    if (Others.IndexOf("+no_voice") == -1 && NoVoice)
                    {
                        sw.WriteLine("+no_voice");
                    }
                    if (Others.IndexOf("+port") == -1 && Port != 0)
                    {
                        sw.WriteLine("+port " + Port);
                    }
                    if (Others.IndexOf("+high_update") == -1 && Others.IndexOf("+datarate") == -1 && Others.IndexOf("+lan_update") == -1 && Others.IndexOf("+med_update") == -1 && Others.IndexOf("+low_update") == -1)
                    {
                        sw.WriteLine("+lan_update");
                    }
                    foreach (string o in Others)
                    {
                        sw.WriteLine(o);
                    }
                }
            }catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "MultiCfg.SaveData()", ex);
            }
        }
    }
}
