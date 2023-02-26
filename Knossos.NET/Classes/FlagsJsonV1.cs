using System.Collections.Generic;

namespace Knossos.NET.Classes
{
    public class FlagsJsonV1
    {
        public Version? version { get; set; }
        public Openal? openal { get; set; }
        public List<Joystick>? joysticks { get; set; }
        public List<string>? easy_flags { get; set; }
        public List<string>? voices { get; set; }
        public List<Flag>? flags { get; set; }
        public List<string>? caps { get; set; }
        public List<Display>? displays { get; set; }
        public string pref_path { get; set; } = string.Empty;
    }

    public class Version
    {
        public string full { get; set; } = string.Empty;
        public string revision_str { get; set; } = string.Empty;
        public int major { get; set; }
        public int minor { get; set; }
        public int build { get; set; }
        public bool has_revision { get; set; }
        public int revision { get; set; }
    }

    public class Openal
    {
        public object? efx_support { get; set; }
        public int version_minor { get; set; }
        public int version_major { get; set; }
        public string default_playback { get; set; } = string.Empty;
        public List<string>? capture_devices { get; set; }
        public string default_capture { get; set; } = string.Empty;
        public List<string>? playback_devices { get; set; }
    }

    public class Flag
    {
        public string name { get; set; } = string.Empty;
        public bool fso_only { get; set; }
        public string description { get; set; } = string.Empty;
        public string web_url { get; set; } = string.Empty;
        public int on_flags { get; set; }
        public string type { get; set; } = string.Empty;
        public int off_flags { get; set; }
    }

    public class Display
    {
        public int index { get; set; }
        public int height { get; set; }
        public string name { get; set; } = string.Empty;
        public int x { get; set; }
        public int y { get; set; }
        public int width { get; set; }
        public List<Mode>? modes { get; set; }
    }

    public class Mode
    {
        public int x { get; set; }
        public int y { get; set; }
        public int bits { get; set; }
    }

    public class Joystick
    {
        public string name { get; set; } = string.Empty;
        public string guid { get; set; } = string.Empty;
        public int id { get; set; }
        public int num_hats { get; set; }
        public int device { get; set; }
        public int num_axes { get; set; }
        public int num_balls { get; set; }
        public int num_buttons { get; set; }
        public bool is_haptic { get; set; }
    }
}

