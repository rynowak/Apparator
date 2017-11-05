using System;
using Microsoft.Build.Framework;

namespace Apparator.Messages
{
    [Serializable]
    internal class BuildEventArgsMessage
    {
        public BuildEventArgsMessage()
        {
        }

        public BuildEventArgsMessage(BuildEventArgs other)
        {
            Timestamp = other.Timestamp;
            ThreadId = other.ThreadId;
            Message = other.Message;
            HelpKeyword = other.HelpKeyword;
            SenderName = other.SenderName;
            BuildEventContext = other.BuildEventContext == null ? null : new BuildEventContextMessage(other.BuildEventContext);
        }

        public DateTime Timestamp { get; set; }

        public int ThreadId { get; set; }

        public string Message { get; set; }

        public string HelpKeyword { get; set; }

        public string SenderName { get; set; }

        public BuildEventContextMessage BuildEventContext { get; set; }

        [Serializable]
        public class BuildEventContextMessage
        {
            public BuildEventContextMessage(BuildEventContext other)
            {
                ProjectInstanceId = other.ProjectInstanceId;
                TaskId = other.TaskId;
                ProjectContextId = other.ProjectContextId;
                TargetId = other.TargetId;
                BuildRequestId = other.BuildRequestId;
                SubmissionId = other.SubmissionId;
                NodeId = other.NodeId;
            }

            public int ProjectInstanceId { get; set; }

            public int TaskId { get; set; }

            public int ProjectContextId { get; set; }

            public int TargetId { get; set; }

            public long BuildRequestId { get; set; }

            public int SubmissionId { get; set; }

            public int NodeId { get; set; }

            public BuildEventContext ToEventArgs()
            {
                return new BuildEventContext(SubmissionId, NodeId, ProjectInstanceId, ProjectContextId, TargetId, TaskId);
            }
        }
    }
}
