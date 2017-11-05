using System;
using Microsoft.Build.Framework;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Apparator.Razor.Tasks
{
    public class Rzc : MSBuildTask
    {
        [Required]
        public ITaskItem[] References { get; set; }

        [Required]
        public string OutputAssembly { get; set; }

        public override bool Execute()
        {
            foreach (var reference in References)
            {
                Log.LogMessage(MessageImportance.High, "reference assembly: {0}", reference.ItemSpec);
            }

            Log.LogMessage(MessageImportance.High, "output assembly: {0}", OutputAssembly);

            return true;
        }
    }
}
