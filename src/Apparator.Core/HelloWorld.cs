using Microsoft.Build.Framework;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Apparator.Core
{
    public class HelloWorld : MSBuildTask
    {
        [Required]
        public string Name { get; set; }

        public override bool Execute()
        {
            Log.LogMessage("Hello {0}", Name);
            return true;
        }
    }
}
