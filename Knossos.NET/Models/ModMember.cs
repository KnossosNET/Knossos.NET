using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knossos.NET.Models
{
    /// <summary>
    /// Nebula int values, do not change
    /// It is not clear what can each one of the roles do and what not
    /// </summary>
    public enum ModMemberRole
    {
        Owner = 0,
        Manager = 10,
        Uploader = 20,
        Tester = 30
    }

    public class ModMember
    {
        public ModMemberRole role { get; set; }
        public string user { get; set; } = string.Empty;
    }
}
