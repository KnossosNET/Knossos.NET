using Knossos.NET.Classes;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Knossos.NET.Models
{
    public class ModDependency
    {
        public string id { get; set; } = string.Empty; // required
        public string? version { get; set; } // required, https://getcomposer.org/doc/01-basic-usage.md#package-versions
        public List<string>? packages { get; set; } // optional, specifies which optional and recommended packages are also required

        /// <summary>
        /// Used to store the original dependency reference during mod.GetMissingDependencies
        /// Useful to track down the original pkg this dependency belongs to
        /// mod.FindPackageWithDependency()
        /// Not saved in Json
        /// </summary>
        [JsonIgnore]
        public ModDependency? originalDependency;

        /// <summary>
        /// Returns the best installed mod that meets this dependency by semantic version, null if none.
        /// Also returns null if the ID is a FSO build.
        /// </summary>
        /// <returns>Mod or null</returns>
        public Mod? SelectMod()
        {
            var mods = Knossos.GetInstalledModList(id);
            Mod? validMod = null;

            foreach (var mod in mods)
            {

                try
                {
                    if (SemanticVersion.SastifiesDependency(version, mod.version))
                    {
                        if (validMod == null)
                        {
                            validMod = mod;
                        }
                        else
                        {
                            if (SemanticVersion.Compare(mod.version, validMod.version) > 0)
                            {
                                validMod = mod;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Add(Log.LogSeverity.Error, "Mod.SelectMod()", "Mod: " + mod.title + " " + ex.Message);
                }
            }
            return validMod;
        }

        /// <summary>
        /// Returns the best available mod on nebula that meets this dependency by semantic version, null if none.
        /// Takes an optional list with all mods, if passed it will use that list intead of checking the repo.json again
        /// </summary>
        /// <param name="mods"></param>
        /// <returns>Mod or null</returns>
        public async Task<Mod?> SelectModNebula(List<Mod>? mods = null)
        {
            if (mods == null)
            {
                mods = await Nebula.GetAllModsWithID(id);
            }

            Mod? validMod = null;

            foreach (var mod in mods)
            {
                if (mod.id == id && SemanticVersion.SastifiesDependency(version, mod.version))
                {
                    if (validMod == null)
                    {
                        validMod = mod;
                    }
                    else
                    {
                        if (mod.type == ModType.engine)
                        {
                            var stabilityV = FsoBuild.GetFsoStability(validMod.stability, validMod.id);
                            var stabilityM = FsoBuild.GetFsoStability(mod.stability, mod.id);
                            //inverted stability comparison
                            if (stabilityV > stabilityM || stabilityV == stabilityM && SemanticVersion.Compare(mod.version, validMod.version) > 0)
                            {
                                validMod = mod;
                            }
                        }
                        else
                        {
                            if (SemanticVersion.Compare(mod.version, validMod.version) > 0)
                            {
                                validMod = mod;
                            }
                        }
                    }
                }
            }

            return validMod;
        }

        /// <summary>
        /// Returns the best installed build that meets this dependency by semantic version, null if none.
        /// Optional: Only select builds with at least one valid executabl
        /// </summary>
        /// <returns>FsoBuild or null</returns>
        public FsoBuild? SelectBuild(bool onlyWithValidExecutable = false)
        {
            FsoBuild? validBuild = null;

            foreach (var build in Knossos.GetInstalledBuildsList(id, FsoStability.Stable))
            {
                if ((!onlyWithValidExecutable || onlyWithValidExecutable && build.IsValidBuild()) && SemanticVersion.SastifiesDependency(version, build.version))
                {

                    if (validBuild == null)
                    {
                        validBuild = build;
                    }
                    else
                    {
                        if (SemanticVersion.Compare(build.version, validBuild.version) > 0)
                        {
                            validBuild = build;
                        }
                    }
                }
            }

            if (validBuild == null)
            {
                foreach (var build in Knossos.GetInstalledBuildsList(id, FsoStability.RC))
                {
                    if ((!onlyWithValidExecutable || onlyWithValidExecutable && build.IsValidBuild()) && SemanticVersion.SastifiesDependency(version, build.version))
                    {

                        if (validBuild == null)
                        {
                            validBuild = build;
                        }
                        else
                        {
                            if (SemanticVersion.Compare(build.version, validBuild.version) > 0)
                            {
                                validBuild = build;
                            }
                        }
                    }
                }
            }

            if (validBuild == null)
            {
                foreach (var build in Knossos.GetInstalledBuildsList(id, FsoStability.Nightly))
                {
                    if ((!onlyWithValidExecutable || onlyWithValidExecutable && build.IsValidBuild()) && SemanticVersion.SastifiesDependency(version, build.version))
                    {

                        if (validBuild == null)
                        {
                            validBuild = build;
                        }
                        else
                        {
                            if (SemanticVersion.Compare(build.version, validBuild.version) > 0)
                            {
                                validBuild = build;
                            }
                        }
                    }
                }
            }

            if (validBuild == null)
            {
                foreach (var build in Knossos.GetInstalledBuildsList(id, FsoStability.Custom))
                {
                    if ((!onlyWithValidExecutable || onlyWithValidExecutable && build.IsValidBuild()) && SemanticVersion.SastifiesDependency(version, build.version))
                    {

                        if (validBuild == null)
                        {
                            validBuild = build;
                        }
                        else
                        {
                            if (SemanticVersion.Compare(build.version, validBuild.version) > 0)
                            {
                                validBuild = build;
                            }
                        }
                    }
                }
            }

            return validBuild;
        }

        /// <summary>
        /// returns id + version
        /// </summary>
        public override string ToString()
        {
            return id + " " + version;
        }
    }
}
