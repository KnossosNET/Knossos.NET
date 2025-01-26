using HarfBuzzSharp;
using Knossos.NET.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knossos.NET.Classes
{
    public class ModTag
    {
        public ModTag()
        {

        }

        public ModTag(string modID)
        {
            ModID = modID;
        }

        public ModTag(string modID, string tag)
        {
            ModID = modID;
            Tags.Add(tag.ToLower());
        }

        public ModTag(string modID, List<string> tags)
        {
            ModID = modID;
            Tags = tags;
        }

        public void AddTag(string tag)
        {
            Tags.Add(tag.ToLower());
        }

        public bool TagExist(string tag)
        {
            return Tags.Contains(tag.ToLower());
        }

        public List<string> GetTags()
        {
            return Tags;
        }

        public string ModID { get; private set; } = string.Empty;
        private List<string> Tags { get; set; } = new List<string>();
    }

    /// <summary>
    /// Handlers for the mod tag system
    /// Each mod id can contain any number of tags
    /// Tags are loaded from nebula and local locals at start
    /// Then we can check here if a mod ID contains a tag
    /// Note:
    /// tags are case insensitive, but ModIDs arent!
    /// </summary>
    public static class ModTags
    {
        private static List<ModTag> modTags = new List<ModTag>();

        /// <summary>
        /// Use _ for spaces and remember these are checked with string.compare so no tags that can contain another
        /// </summary>
        public enum Tags
        {
            Total_Conversion,
            Retail_FS2,
            FS2_MOD,
            FS1_MOD,
            TC_MOD,
            Utility,
            Dependency,
            VR_MOD,
            MediaVPs,
            Demo,
            Multiplayer,
            Testing
        }

        public static void ClearTags()
        {
            modTags.Clear();
        }

        /// <summary>
        /// Get a list of all tags loaded, without the mod id
        /// </summary>
        /// <returns>List<string></returns>
        public static List<string> GetListAllTags()
        {
            List<string> list = new List<string>();
            foreach (var modtag in modTags)
            {
                foreach (var tags in modtag.GetTags())
                {
                    if (!list.Contains(tags))
                        list.Add(tags);
                }
            }
            return list;
        }

        /// <summary>
        /// Checks if a mod id contains a tag
        /// </summary>
        /// <param name="modid"></param>
        /// <param name="tag"></param>
        /// <returns>true/false</returns>
        public static bool IsTagPresentInModID(string modid, string tag)
        {
            var idTags = modTags.FirstOrDefault(x => x.ModID == modid);
            if (idTags != null)
            {
                return idTags.TagExist(tag);
            }
            return false;
        }

        /// <summary>
        /// Adds a mod tag to the list
        /// If the id and tag dosent exist already
        /// </summary>
        /// <param name="id"></param>
        /// <param name="tag"></param>
        public static void AddModTag(string id, string tag)
        {
            var idFound = modTags.FirstOrDefault(x => x.ModID == id);
            if (idFound != null)
            {
                if (!idFound.TagExist(tag))
                    idFound.AddTag(tag);
            }
            else
            {
                modTags.Add(new ModTag(id, tag));
            }
        }

        /// <summary>
        /// Generate runtime mod tags for this mod
        /// Tags will be determined based on avalible mod information
        /// </summary>
        /// <param name="mod"></param>
        public static void AddModTagsRuntime(Mod? mod)
        {
            if(mod == null || mod.id.ToLower() == "fs2") return;
            if(mod.parent == null)
            {
                AddModTag(mod.id,Tags.Total_Conversion.ToString());
            }
            else
            {
                if (mod.parent.ToLower() == "fs2")
                {
                    if(mod.GetDependency("fsport", true) != null)
                    {
                        AddModTag(mod.id, Tags.FS1_MOD.ToString()); // only going to work for installed mods
                    }
                    else
                    {
                        AddModTag(mod.id, Tags.FS2_MOD.ToString());
                    }
                }
                else
                {
                    AddModTag(mod.id, Tags.TC_MOD.ToString());
                }
            }
        }

        /// <summary>
        /// List of harcoded tags, this is temporal until tags are implemented in nebula
        /// </summary>
        public static void AddHardcodedModTags()
        {
            AddModTag("FS2", Tags.Retail_FS2.ToString());

            AddModTag("fsport", Tags.FS1_MOD.ToString());
            AddModTag("fsport-mediavps", Tags.FS1_MOD.ToString());
            AddModTag("fsport-mediavps", Tags.MediaVPs.ToString());
            AddModTag("str", Tags.FS1_MOD.ToString());
            AddModTag("denebiii", Tags.FS1_MOD.ToString());
            AddModTag("awakenings", Tags.FS1_MOD.ToString());
            AddModTag("the_aftermath_ribos", Tags.FS1_MOD.ToString());
            AddModTag("RetreatfromDenebCinematic", Tags.FS1_MOD.ToString());
            AddModTag("tombaugh_attack_cinematic", Tags.FS1_MOD.ToString());

            AddModTag("MjnMHs", Tags.Dependency.ToString());
            AddModTag("SCPUI", Tags.Dependency.ToString());

            AddModTag("Wing_Commander_Saga", Tags.Total_Conversion.ToString());

            AddModTag("MVPS", Tags.MediaVPs.ToString());
            AddModTag("MVPS", Tags.FS2_MOD.ToString());

            AddModTag("BWO_Demo", Tags.FS2_MOD.ToString());
            AddModTag("BWO_Demo", Tags.Demo.ToString());

            AddModTag("fs2_demo", Tags.FS2_MOD.ToString());
            AddModTag("fs2_demo", Tags.Demo.ToString());
            AddModTag("fs2_org_demo", Tags.Demo.ToString());
            AddModTag("fs2_org_demo", Tags.Total_Conversion.ToString());

            AddModTag("WCIV_Demo", Tags.Demo.ToString());
            AddModTag("WCIV_Demo", Tags.TC_MOD.ToString());

            AddModTag("Solaris", Tags.Total_Conversion.ToString());
            AddModTag("wod", Tags.Total_Conversion.ToString());
            AddModTag("Diaspora_Release_Version", Tags.Total_Conversion.ToString());

            AddModTag("rogue", Tags.FS2_MOD.ToString());
            AddModTag("blueplanetcomplete", Tags.FS2_MOD.ToString());

            AddModTag("fs1coopup", Tags.FS1_MOD.ToString());
            AddModTag("fs1coopup", Tags.Multiplayer.ToString());
            AddModTag("fs2coopup", Tags.FS2_MOD.ToString());
            AddModTag("fs2coopup", Tags.Multiplayer.ToString());
            AddModTag("strcoopup", Tags.FS1_MOD.ToString());
            AddModTag("strcoopup", Tags.Multiplayer.ToString());

            AddModTag("frontlines", Tags.FS1_MOD.ToString());
            AddModTag("jad", Tags.FS2_MOD.ToString());

            AddModTag("BTA_Standalone", Tags.Total_Conversion.ToString());
            AddModTag("BtA", Tags.FS2_MOD.ToString());
            AddModTag("BtA", Tags.TC_MOD.ToString());

            AddModTag("vr_mvps", Tags.VR_MOD.ToString());
            AddModTag("vr_mvps_fsport", Tags.VR_MOD.ToString());
            AddModTag("VRGC", Tags.VR_MOD.ToString());

            AddModTag("CP_m", Tags.Utility.ToString());
            AddModTag("CP_M_FS1", Tags.Utility.ToString());

            AddModTag("mlteset", Tags.Testing.ToString());
            AddModTag("ParticlesStressTesting", Tags.Testing.ToString());
            AddModTag("STIG", Tags.Testing.ToString());
            AddModTag("qaz_1", Tags.Testing.ToString());
            AddModTag("itsatestnumbnuts", Tags.Testing.ToString());
            AddModTag("FSPcustom", Tags.Testing.ToString());
            AddModTag("Stress_Test_Multi_With_Silly_mission", Tags.Testing.ToString());
            AddModTag("UITest", Tags.Testing.ToString());
        }

    }
}
