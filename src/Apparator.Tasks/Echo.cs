using Microsoft.Build.Framework;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Apparator.Tasks
{
    public class Echo : MSBuildTask
    {
        private readonly MessageImportance DefaultLevel = MessageImportance.High;

        [Required]
        public string Message { get; set; }

        [Output]
        public string Response { get; set; }

        public override bool Execute()
        {
            Log.LogMessage(DefaultLevel, "Echo task called with '{0}'", Message);

            Response = $"Hey, {Message} - have a weekend.";
            return true;
        }
    }
}