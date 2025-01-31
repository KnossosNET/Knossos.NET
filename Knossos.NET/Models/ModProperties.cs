namespace Knossos.NET.Models
{
    /// <summary>
    /// This is generated from the enviroment string at runtime
    /// </summary>
    public class ModProperties
    {
        public bool x64 { get; set; }
        public bool sse2 { get; set; }
        public bool avx { get; set; }
        public bool avx2 { get; set; }

        /* Knossos.NET added */
        public bool arm64 { get; set; }
        public bool arm32 { get; set; }
        public bool riscv32 { get; set; }
        public bool riscv64 { get; set; }
        public bool other { get; set; }
    }
}
