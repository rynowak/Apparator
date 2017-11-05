using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Apparator.Tasks
{
    public class SayHello : MSBuildTask
    {
        private readonly MessageImportance DefaultLevel = MessageImportance.High;

        [Required]
        public string Message { get; set; }

        [Required]
        public ITaskItem[] Individuals { get; set; }

        [Output]
        public ITaskItem[] Responses { get; set; }

        public override bool Execute()
        {
            Log.LogMessage(DefaultLevel, "Echo task called with '{0}'", Message);

            Responses = new ITaskItem[Individuals.Length];
            for (var i = 0; i < Individuals.Length; i++)
            {
                var individual = Individuals[i];
                Responses[i] = new TaskItem(string.Format(Message, individual.ItemSpec), individual.CloneCustomMetadata());
            }
            return true;
        }
    }
}
