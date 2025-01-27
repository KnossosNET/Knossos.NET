using Knossos.NET.Models;
using System.Collections.Generic;
using System.Linq;

namespace Knossos.NET.Classes
{
    public class ModTag
    {
        public ModTag(string modID, string? filter = null, string? tag = null)
        {
            ModID = modID;
            if(filter != null)
                Filters.Add(filter.ToLower());
            if(tag != null)
                Tags.Add(tag.ToLower());
        }

        public void AddFilter(string filter)
        {
            //special cases:
            //do not add FS2_MOD filter if FS1_MOD exist
            //Remove FS2_MOD if FS1_MOD mod is added
            if (filter.ToLower() == "fs1_mod")
                Filters.Remove("fs2_mod");
            if (filter.ToLower() == "fs2_mod" && FilterExist("fs1_mod"))
                return;
            Filters.Add(filter.ToLower());
        }

        public bool FilterExist(string filter)
        {
            return Filters.Contains(filter.ToLower());
        }

        public List<string> GetFilters()
        {
            return Filters;
        }

        public void AddTag(string tag)
        {
            Tags.Add(tag.ToLower());
        }

        public bool TagExist(string tag)
        {
            if(Tags.Any())
                return Tags.Contains(tag.ToLower());
            else
                return false;
        }

        public string ModID { get; private set; } = string.Empty;
        private List<string> Filters { get; set; } = new List<string>();
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
        public enum Filters
        {
            Total_Conversion,
            Retail_FS2,
            FS2_MOD,
            FS1_MOD,
            TC_MOD,
            Utility,
            Dependency,
            VR_MOD,
            Asset_Pack,
            Demo,
            Multiplayer,
            Testing
        }

        /// <summary>
        /// Clear the all loaded tags and filters
        /// </summary>
        public static void Clear()
        {
            modTags.Clear();
        }

        /// <summary>
        /// Get a list of all filters loaded, without the mod id
        /// </summary>
        /// <returns>List<string></returns>
        public static List<string> GetListAllFilters()
        {
            List<string> list = new List<string>();
            for (int i = 0; i < modTags.Count(); i++)
            {
                var filters = modTags[i].GetFilters();
                for (int j = 0; j < filters.Count(); j++)
                {
                    if (!list.Contains(filters[j]))
                        list.Add(filters[j]);
                }
            }
            return list;
        }

        /// <summary>
        /// Checks if a mod id contains a filter
        /// </summary>
        /// <param name="modid"></param>
        /// <param name="filter"></param>
        /// <returns>true/false</returns>
        public static bool IsFilterPresentInModID(string modid, string filter)
        {
            for (int i = 0; i < modTags.Count(); i++)
            {
                if (modTags[i].ModID == modid)
                {
                    return modTags[i].FilterExist(filter);
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if a mod id contains a tag
        /// </summary>
        /// <param name="modid"></param>
        /// <param name="tag"></param>
        /// <returns>true/false</returns>
        public static bool IsTagPresentInModID(string modid, string tag)
        {
            for (int i = 0; i < modTags.Count(); i++)
            {
                if (modTags[i].ModID == modid)
                {
                    return modTags[i].TagExist(tag);
                }
            }
            return false;
        }

        /// <summary>
        /// Adds a mod filter to the list
        /// If the id and filter dosent exist already
        /// </summary>
        /// <param name="id"></param>
        /// <param name="filter"></param>
        public static void AddModFilter(string id, string filter)
        {
            bool found = false;
            for (int i = 0; i < modTags.Count(); i++)
            {
                if (modTags[i].ModID == id)
                {
                    if(!modTags[i].FilterExist(filter))
                    {
                        modTags[i].AddFilter(filter);
                        found = true;
                        break;
                    }
                }
            }
            if(!found)
            {
                modTags.Add(new ModTag(id, filter));
            }
        }

        /// <summary>
        /// Adds a mod tag to the list
        /// If the id and tag dosent exist already
        /// </summary>
        /// <param name="id"></param>
        /// <param name="filter"></param>
        public static void AddModTag(string id, string tag)
        {
            bool found = false;
            for (int i = 0; i < modTags.Count(); i++)
            {
                if (modTags[i].ModID == id)
                {
                    if (!modTags[i].TagExist(tag))
                    {
                        modTags[i].AddTag(tag);
                        found = true;
                        break;
                    }
                }
            }
            if (!found)
            {
                modTags.Add(new ModTag(id, null, tag));
            }
        }

        /// <summary>
        /// Generate runtime mod tags for this mod
        /// Tags will be determined based on avalible mod information
        /// </summary>
        /// <param name="mod"></param>
        public static void AddModFiltersRuntime(Mod? mod)
        {
            if(mod == null) return;
            if(mod.id.ToLower() == "fs2")
            {
                AddModFilter(mod.id, Filters.Retail_FS2.ToString());
            }
            if(mod.parent == null)
            {
                AddModFilter(mod.id,Filters.Total_Conversion.ToString());
            }
            else
            {
                if (mod.parent.ToLower() == "fs2")
                {
                    if(mod.GetDependency("fsport", true) != null)
                    {
                        AddModFilter(mod.id, Filters.FS1_MOD.ToString()); // only going to work for installed mods
                    }
                    else
                    {
                        AddModFilter(mod.id, Filters.FS2_MOD.ToString());
                    }
                }
                else
                {
                    AddModFilter(mod.id, Filters.TC_MOD.ToString());
                }
            }
        }

        /// <summary>
        /// List of harcoded filters, this is temporal until tags are implemented in nebula
        /// </summary>
        public static void AddHardcodedModFilters()
        {
            AddModFilter("FS2", Filters.Retail_FS2.ToString());
            AddModFilter("fsport", Filters.FS1_MOD.ToString());
            AddModFilter("fsport-mediavps", Filters.FS1_MOD.ToString());
            AddModFilter("fsport-mediavps", Filters.Asset_Pack.ToString());
            AddModFilter("str", Filters.FS1_MOD.ToString());
            AddModFilter("denebiii", Filters.FS1_MOD.ToString());
            AddModFilter("awakenings", Filters.FS1_MOD.ToString());
            AddModFilter("the_aftermath_ribos", Filters.FS1_MOD.ToString());
            AddModFilter("RetreatfromDenebCinematic", Filters.FS1_MOD.ToString());
            AddModFilter("tombaugh_attack_cinematic", Filters.FS1_MOD.ToString());
            AddModFilter("MjnMHs", Filters.Dependency.ToString());
            AddModFilter("SCPUI", Filters.Dependency.ToString());
            AddModFilter("MVPS", Filters.Asset_Pack.ToString());
            AddModFilter("BWO_Demo", Filters.Demo.ToString());
            AddModFilter("fs2_demo", Filters.Demo.ToString());
            AddModFilter("fs2_org_demo", Filters.Demo.ToString());
            AddModFilter("WCIV_Demo", Filters.Demo.ToString());
            AddModFilter("fs1coopup", Filters.FS1_MOD.ToString());
            AddModFilter("fs1coopup", Filters.Multiplayer.ToString());
            AddModFilter("fs2coopup", Filters.Multiplayer.ToString());
            AddModFilter("strcoopup", Filters.FS1_MOD.ToString());
            AddModFilter("strcoopup", Filters.Multiplayer.ToString());
            AddModFilter("frontlines", Filters.FS1_MOD.ToString());
            AddModFilter("vr_mvps", Filters.VR_MOD.ToString());
            AddModFilter("vr_mvps_fsport", Filters.VR_MOD.ToString());
            AddModFilter("VRGC", Filters.VR_MOD.ToString());
            AddModFilter("CP_m", Filters.Utility.ToString());
            AddModFilter("CP_M_FS1", Filters.Utility.ToString());
            AddModFilter("mlteset", Filters.Testing.ToString());
            AddModFilter("ParticlesStressTesting", Filters.Testing.ToString());
            AddModFilter("STIG", Filters.Testing.ToString());
            AddModFilter("qaz_1", Filters.Testing.ToString());
            AddModFilter("itsatestnumbnuts", Filters.Testing.ToString());
            AddModFilter("FSPcustom", Filters.Testing.ToString());
            AddModFilter("Stress_Test_Multi_With_Silly_mission", Filters.Testing.ToString());
            AddModFilter("UITest", Filters.Testing.ToString());
        }

    }
}
