using System;

namespace Knossos.NET.Classes
{
    /*
        Helper class to handle semantic versions used by FSO and Knossos.
        Just the basics needed to do comparisons without having to include 
        a 3rd party library for this.
    */
    public class SemanticVersion
    {
        int major;
        int minor;
        int revision;
        string? prerelease;

        public SemanticVersion(string version)
        {
            try
            {
                if (version == null)
                {
                    throw new ArgumentNullException("version can not be null");
                }
                if (version.Trim().Length == 0)
                {
                    major = 0;
                    minor = 0;
                    revision = 0;
                    prerelease = null;
                }
                else
                {
                    string[] parts = version.Replace(" ", "").Split('-')[0].Split('.');

                    if (parts.Length == 3)
                    {
                        try
                        {
                            major = int.Parse(parts[0]);
                            minor = int.Parse(parts[1]);
                            revision = int.Parse(parts[2]);
                        }
                        catch
                        {
                            throw new Exception("version has a invalid semantic format: " + version);
                        }
                    }
                    else
                    {
                        throw new Exception("version has a invalid semantic format: " + version);
                    }

                    if (version.Contains("-"))
                    {
                        prerelease = version.Replace(" ", "").Split('-')[1];
                    }
                    else
                    {
                        prerelease = null;
                    }
                }
            }catch(Exception e) 
            {
                major=0;
                minor = 0;
                revision = 0;
                prerelease = null;
                Log.Add(Log.LogSeverity.Error, "SemanticVersion(string version)",e);
            }
        }

        /*
            Compare two semantic version strings.
            I decided for a static comparison function instead of operator overload because
            it allowed to resolve all requeriments in a single method and also allowed to take
            strings. If overloading is necessary it can be added at any time.
            Retuns >=1 if VersionA is superior, 0 if equal or <=-1 if Version A is inferior.
        */
        public static int Compare(string versionA, string versionB)
        {
            return Compare(new SemanticVersion(versionA), new SemanticVersion(versionB));
        }


        /*
            Compare two semantic versions.
            I decided for a static comparison function instead of operator overload because
            it allowed to resolve all requeriments in a single method and also allowed to take
            strings. If overloading is necessary it can be added at any time.
            Retuns >=1 if VersionA is superior, 0 if equal or <=-1 if Version A is inferior.
        */
        public static int Compare(SemanticVersion versionA, SemanticVersion versionB)
        {
            if(versionA.major != versionB.major)
            {
                return versionA.major - versionB.major;
            }
            else
            {
                if(versionA.minor != versionB.minor)
                {
                    return versionA.minor - versionB.minor;
                }
                else
                {
                    if(versionA.revision != versionB.revision)
                    {
                        return versionA.revision - versionB.revision;
                    }
                    else
                    {
                        if(versionA.prerelease == null && versionB.prerelease != null)
                        {
                            return 1;
                        }

                        if(versionA.prerelease != null && versionB.prerelease == null)
                        {
                            return -1;
                        }

                        return string.Compare(versionA.prerelease, versionB.prerelease);
                    }
                }
            }
        }

        /*
            Compares a semantic version string to the version string in the mod dependency to see if it sastifies the requirement.
            Returns true or false
            Version : null -> Any, Version: "4.6.1" -> Only that version, Version: "~4.6.1" -> >=4.6.1 < 4.7.0, Version: ">=4.6.1"->equal or better
        */
        public static bool SastifiesDependency(string? dependencyVersion, string? version)
        {
            if (version == null)
                return false;

            return SastifiesDependency(dependencyVersion, new SemanticVersion(version));
        }

        /*
            Compares a semantic version to the version string in the mod dependency to see if it sastifies the requirement.
            Returns true or false
            Version : null -> Any, Version: "4.6.1" -> Only that version, Version: "~4.6.1" -> >=4.6.1 < 4.7.0, Version: ">=4.6.1"->equal or better
        */
        public static bool SastifiesDependency(string? dependencyVersion, SemanticVersion version)
        {
            try
            {
                /* If dependencyversion is null it means any version will do and the mod will use the newerest installed version avalible */
                if (dependencyVersion == null)
                {
                    return true;
                }

                if (dependencyVersion.Contains("~"))
                {
                    var versionDep = new SemanticVersion(dependencyVersion.Replace("~", ""));
                    /* major and minor has to math, revision needs to be equal or superior*/
                    if (version.major == versionDep.major && version.minor == versionDep.minor && version.revision >= versionDep.revision)
                    {
                        if (Compare(version, versionDep) >= 0)
                        {
                            return true;
                        }
                    }
                    return false;
                }

                if (dependencyVersion.Contains(">="))
                {
                    var versionDep = new SemanticVersion(dependencyVersion.Replace(">=", ""));
                    /* major minor and revision needs to be equal or superior*/
                    if (version.major >= versionDep.major || version.major == versionDep.major && version.minor >= versionDep.minor || version.major == versionDep.major && version.minor == versionDep.minor && version.revision >= versionDep.revision)
                    {
                        if (Compare(version, versionDep) >= 0)
                        {
                            return true;
                        }
                    }

                    return false;
                }

                if (dependencyVersion.Contains("<="))
                {
                    var versionDep = new SemanticVersion(dependencyVersion.Replace("<=", ""));
                    /* major minor and revision needs to be equal or inferior*/
                    if (version.major <= versionDep.major || version.major == versionDep.major && version.minor <= versionDep.minor || version.major == versionDep.major && version.minor == versionDep.minor && version.revision <= versionDep.revision)
                    {
                        if (Compare(version, versionDep) <= 0)
                        {
                            return true;
                        }
                    }

                    return false;
                }

                if (Compare(version, new SemanticVersion(dependencyVersion)) == 0)
                {
                    return true;
                }

                return false;
            }catch (Exception ex) 
            {
                Log.Add(Log.LogSeverity.Error, "SemanticVersion.SastifiesDependency()", ex);
                return false; 
            }
        }

        public override string ToString()
        {
            if(prerelease != null)
            {
                return major + "." + minor + "." + revision + "-" + prerelease;
            }
            else
            {
                return major + "." + minor + "." + revision;
            }
        }
    }
}
