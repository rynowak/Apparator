using Microsoft.Build.Framework;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Apparator.Core
{
    public partial class Apparate : MSBuildTask
    {
        public MessageImportance MessageImportance { get; set; } = MessageImportance.High;

        public bool Debug { get; set; }

        [Required]
        public string DepsFile { get; set; }

        [Required]
        public string EntryAssembly { get; set; }

        // Needed because the Razor compiler is not a runtime library.
        public string EntryAssemblyPath { get; set; }

        [Required]
        public string EntryPointType { get; set; }

        [Required]
        public string EntryPointMethod { get; set; }

        [Required]
        public string[] Args { get; set; }
    }
}
